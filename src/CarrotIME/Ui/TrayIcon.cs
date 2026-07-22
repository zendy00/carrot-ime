using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using CarrotIME.Adapters;

namespace CarrotIME.Ui;

/// <summary>
/// 시스템 트레이 아이콘 + 메뉴(일시정지/재개, Windows 시작 시 실행, 종료).
/// 아이콘에는 현재 입력 상태 글자(한/영/あ 등)를 그려 넣는다.
/// 상태 변경은 콜백으로 밖에 알린다(설정 저장·컨트롤러 제어는 호출부가 담당).
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private static readonly Color CarrotBody = Color.FromArgb(237, 106, 44);   // 주황 몸통
    private static readonly Color CarrotLeaf = Color.FromArgb(76, 175, 80);    // 초록 잎
    private static readonly Color GlyphColor = Color.White;                    // 흰색 글자
    private const int IconSize = 32; // 고DPI에서도 글자가 또렷하도록 크게 그려 Windows가 축소

    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly ToolStripMenuItem _idleMenu;
    private readonly ToolStripTextBox _customIdleBox;
    private readonly Font _iconFont = new("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
    private IntPtr _hIcon;
    private string _glyph = string.Empty;

    // 표시 지연 시간 프리셋(초). 0 = 항상 표시.
    private static readonly int[] IdlePresets = { 0, 1, 2, 3, 5, 10, 30 };

    // 직접 입력 상한(초). 600 이상을 입력하면 이 값으로 잘라 적용한다.
    private const int MaxIdleSeconds = 599;

    private const string RepoUrl = "https://github.com/zendy00/carrot-ime";

    public TrayIcon(
        bool enabled, bool autoStart, int idleSeconds, Color indicatorColor, Color textColor,
        int opacityPercent,
        Action<bool> onEnabledChanged, Func<bool, bool> onAutoStartChanged,
        Action<int> onIdleSecondsChanged, Action<Color> onColorChanged,
        Action<Color> onTextColorChanged, Action<int> onOpacityChanged, Action onExit)
    {
        // 체크 = 일시정지 상태(=꺼짐). CheckOnClick으로 Click 전에 Checked가 갱신됨.
        _pauseItem = new ToolStripMenuItem("Pause") { Checked = !enabled, CheckOnClick = true };
        _pauseItem.Click += (_, _) => onEnabledChanged(!_pauseItem.Checked);

        _autoStartItem = new ToolStripMenuItem("Run at startup") { Checked = autoStart, CheckOnClick = true };
        // 콜백이 실제 적용 결과(성공 여부)를 돌려주면 체크를 현실과 맞춘다.
        _autoStartItem.Click += (_, _) => _autoStartItem.Checked = onAutoStartChanged(_autoStartItem.Checked);

        var menu = new ContextMenuStrip();

        _idleMenu = new ToolStripMenuItem("Reappear delay");
        foreach (int sec in IdlePresets)
        {
            var item = new ToolStripMenuItem(FormatSeconds(sec)) { Tag = sec };
            item.Click += (_, _) =>
            {
                onIdleSecondsChanged(sec);
                UpdateIdleChecks(sec);
            };
            _idleMenu.DropDownItems.Add(item);
        }
        // 프리셋 밖 값은 메뉴 안 텍스트박스로 직접 입력(숫자만, 최대 599초) — 별도 팝업 없음.
        // Enter로 적용. 저장된 값이 프리셋에 없으면 이 박스에 현재값이 표시된다.
        _customIdleBox = new ToolStripTextBox { MaxLength = 3, TextBoxTextAlign = HorizontalAlignment.Right };
        _customIdleBox.TextBox.PlaceholderText = "Custom (s)";
        _customIdleBox.KeyPress += (_, e) =>
            e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar); // 숫자만 허용
        _customIdleBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true; // 엔터 알림음 방지
            if (int.TryParse(_customIdleBox.Text, out int sec))
            {
                sec = Math.Min(sec, MaxIdleSeconds); // 600 이상은 599로 잘라 적용
                onIdleSecondsChanged(sec);
                UpdateIdleChecks(sec);
            }
            menu.Close();
        };
        _idleMenu.DropDownItems.Add(new ToolStripSeparator());
        _idleMenu.DropDownItems.Add(_customIdleBox);
        UpdateIdleChecks(idleSeconds);

        var colorMenu = BuildColorMenu("Indicator color", IndicatorPalette.Swatches, indicatorColor, onColorChanged);
        var textColorMenu = BuildColorMenu("Text color", IndicatorPalette.TextSwatches, textColor, onTextColorChanged);

        // 인디케이터 투명도 — 메뉴 안 슬라이더로 조절(드래그 즉시 반영). 20% 미만은
        // 사실상 안 보여서 하한을 20으로 둔다.
        var opacityMenu = new ToolStripMenuItem(OpacityTitle(opacityPercent));
        var opacityTrack = new TrackBar
        {
            Minimum = 20,
            Maximum = 100,
            SmallChange = 5,
            LargeChange = 10,
            TickStyle = TickStyle.None,
            AutoSize = false,
            Size = new Size(140, 28),
            Value = Math.Clamp(opacityPercent, 20, 100)
        };
        opacityTrack.ValueChanged += (_, _) =>
        {
            onOpacityChanged(opacityTrack.Value);
            opacityMenu.Text = OpacityTitle(opacityTrack.Value);
        };
        opacityMenu.DropDownItems.Add(new ToolStripControlHost(opacityTrack));

        var aboutItem = new ToolStripMenuItem("About…");
        aboutItem.Click += (_, _) => ShowAbout();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => onExit();

        menu.Items.Add(_pauseItem);
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(_idleMenu);
        menu.Items.Add(colorMenu);
        menu.Items.Add(textColorMenu);
        menu.Items.Add(opacityMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(aboutItem);
        menu.Items.Add(exitItem);

        // 열릴 때마다 윈도우 다크/라이트 테마에 맞춰 색을 적용.
        menu.Opening += (_, _) => MenuTheme.Apply(menu);

        _icon = new NotifyIcon
        {
            Icon = CreateGlyphIcon(string.Empty),
            Text = "CarrotIME — input state indicator",
            Visible = true,
            ContextMenuStrip = menu
        };
    }

    private static string FormatSeconds(int sec) =>
        sec == 0 ? "Always" : sec % 60 == 0 ? $"{sec / 60}m" : $"{sec}s";

    private static string OpacityTitle(int percent) => $"Opacity ({percent}%)";

    // 앱 이름·버전·만든이와 GitHub 링크를 보여주는 소형 정보 대화상자.
    private static void ShowAbout()
    {
        using var form = new Form
        {
            Text = "About CarrotIME",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            TopMost = true, // 트레이 메뉴에서 열리므로 다른 창에 가려지지 않게
            ClientSize = new Size(300, 134)
        };
        // 제목 왼쪽에 당근 아이콘(트레이와 동일 그림, 글자 없이)
        using var carrot = new Bitmap(IconSize, IconSize);
        using (var cg = Graphics.FromImage(carrot))
        {
            cg.SmoothingMode = SmoothingMode.AntiAlias;
            DrawCarrot(cg);
        }
        var icon = new PictureBox { Image = carrot, Size = new Size(IconSize, IconSize), Location = new Point(12, 10) };
        var title = new Label
        {
            Text = $"CarrotIME v{Application.ProductVersion}",
            Font = new Font(form.Font, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(52, 18)
        };
        var author = new Label { Text = "Made by : zendy", AutoSize = true, Location = new Point(12, 52) };
        var link = new LinkLabel { Text = RepoUrl, AutoSize = true, Location = new Point(12, 74) };
        link.LinkClicked += (_, _) =>
            Process.Start(new ProcessStartInfo(RepoUrl) { UseShellExecute = true }); // 기본 브라우저로 열기
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(213, 100), Width = 75 };
        form.Controls.AddRange(new Control[] { icon, title, author, link, ok });
        form.AcceptButton = ok;
        form.CancelButton = ok;
        form.ShowDialog();
    }

    // 프리셋은 라디오처럼 하나만 체크. 프리셋 밖 값이면 텍스트박스에 현재값을 표시한다.
    private void UpdateIdleChecks(int sec)
    {
        foreach (var mi in _idleMenu.DropDownItems.OfType<ToolStripMenuItem>())
            if (mi.Tag is int preset)
                mi.Checked = preset == sec;
        bool isPreset = Array.IndexOf(IdlePresets, sec) >= 0;
        _customIdleBox.Text = isPreset ? string.Empty : sec.ToString();
    }

    // 색 선택 서브메뉴: 팔레트(라디오처럼 하나만 체크) + 구분선 + Custom…(표준 컬러 피커).
    // 팔레트 밖 색이면 Custom 항목에 견본을 표시하고 체크한다.
    private static ToolStripMenuItem BuildColorMenu(
        string title, (string Name, Color Color)[] swatches, Color initial, Action<Color> onChanged)
    {
        var menu = new ToolStripMenuItem(title);
        var custom = new ToolStripMenuItem("Custom…");
        Color current = initial; // 컬러 피커를 다시 열 때 초기 색

        void Update(Color color)
        {
            current = color;
            bool isPalette = false;
            foreach (var mi in menu.DropDownItems.OfType<ToolStripMenuItem>())
                if (mi.Tag is Color c)
                {
                    mi.Checked = c.ToArgb() == color.ToArgb();
                    isPalette |= mi.Checked;
                }
            custom.Checked = !isPalette;
            Image? previous = custom.Image;
            custom.Image = isPalette ? null : Swatch(color);
            previous?.Dispose();
        }

        foreach (var (name, color) in swatches)
        {
            var item = new ToolStripMenuItem(name) { Image = Swatch(color), Tag = color };
            item.Click += (_, _) =>
            {
                onChanged(color);
                Update(color);
            };
            menu.DropDownItems.Add(item);
        }
        custom.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = current, FullOpen = true };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                onChanged(dlg.Color);
                Update(dlg.Color);
            }
        };
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add(custom);
        Update(initial);
        return menu;
    }

    // 메뉴에 표시할 색상 견본(작은 원). 앱 수명 동안 유지되므로 별도 dispose 안 함.
    private static Bitmap Swatch(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 13, 13);
        // 흰색 등 밝은 색이 메뉴 배경에 묻히지 않도록 회색 테두리
        using var pen = new Pen(Color.FromArgb(128, 128, 128));
        g.DrawEllipse(pen, 1, 1, 13, 13);
        return bmp;
    }

    /// <summary>현재 입력 상태 글자(한/영/あ 등)를 트레이 아이콘에 반영한다. 바뀔 때만 다시 그린다.</summary>
    public void SetGlyph(string glyph)
    {
        if (glyph == _glyph)
            return;
        _glyph = glyph;

        IntPtr previous = _hIcon;
        _icon.Icon = CreateGlyphIcon(glyph); // _hIcon을 새 핸들로 갱신
        if (previous != IntPtr.Zero)
            Win32.DestroyIcon(previous);      // 이전 GetHicon 핸들 정리
    }

    // 당근(주황 몸통+초록 잎) + 진한 회색 글자로 트레이 아이콘을 런타임 생성.
    private Icon CreateGlyphIcon(string glyph)
    {
        using var bmp = new Bitmap(IconSize, IconSize);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.Clear(Color.Transparent);
            DrawCarrot(g);

            if (glyph.Length > 0)
            {
                using var text = new SolidBrush(GlyphColor);
                using var fmt = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                // 글자는 당근 몸통의 넓은 윗부분에 얹는다.
                g.DrawString(glyph, _iconFont, text, new RectangleF(0, 7, IconSize, 18), fmt);
            }
        }
        _hIcon = bmp.GetHicon();
        return Icon.FromHandle(_hIcon);
    }

    // 32x32 기준 당근: 위에 초록 잎, 아래로 뾰족한 주황 몸통. 폭을 거의 꽉 채워 통통하게.
    private static void DrawCarrot(Graphics g)
    {
        using var leaf = new SolidBrush(CarrotLeaf);
        g.FillEllipse(leaf, 14, 0, 4, 8);   // 가운데 잎
        g.FillEllipse(leaf, 8, 1, 5, 8);    // 왼쪽 잎
        g.FillEllipse(leaf, 19, 1, 5, 8);   // 오른쪽 잎

        using var body = new SolidBrush(CarrotBody);
        var cone = new[]
        {
            new Point(3, 12), new Point(29, 12), new Point(16, 31)
        };
        g.FillPolygon(body, cone);
        g.FillEllipse(body, 3, 5, 26, 13);  // 둥근 윗부분
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _iconFont.Dispose();
        if (_hIcon != IntPtr.Zero)
            Win32.DestroyIcon(_hIcon); // GetHicon으로 만든 핸들 정리
    }
}
