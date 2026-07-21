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
    private static readonly Color GlyphColor = Color.FromArgb(51, 51, 51);     // 진한 회색 글자
    private const int IconSize = 32; // 고DPI에서도 글자가 또렷하도록 크게 그려 Windows가 축소

    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly Font _iconFont = new("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
    private IntPtr _hIcon;
    private string _glyph = string.Empty;

    // 표시 지연 시간 프리셋(초).
    private static readonly int[] IdlePresets = { 5, 10, 20, 30, 60, 180 };

    public TrayIcon(
        bool enabled, bool autoStart, int idleSeconds,
        Action<bool> onEnabledChanged, Func<bool, bool> onAutoStartChanged,
        Action<int> onIdleSecondsChanged, Action onExit)
    {
        // 체크 = 일시정지 상태(=꺼짐). CheckOnClick으로 Click 전에 Checked가 갱신됨.
        _pauseItem = new ToolStripMenuItem("일시정지") { Checked = !enabled, CheckOnClick = true };
        _pauseItem.Click += (_, _) => onEnabledChanged(!_pauseItem.Checked);

        _autoStartItem = new ToolStripMenuItem("Windows 시작 시 실행") { Checked = autoStart, CheckOnClick = true };
        // 콜백이 실제 적용 결과(성공 여부)를 돌려주면 체크를 현실과 맞춘다.
        _autoStartItem.Click += (_, _) => _autoStartItem.Checked = onAutoStartChanged(_autoStartItem.Checked);

        var idleMenu = new ToolStripMenuItem("표시 지연 시간");
        foreach (int sec in IdlePresets)
        {
            var item = new ToolStripMenuItem(FormatSeconds(sec)) { Checked = sec == idleSeconds, Tag = sec };
            item.Click += (_, _) =>
            {
                onIdleSecondsChanged(sec);
                foreach (ToolStripMenuItem mi in idleMenu.DropDownItems)
                    mi.Checked = (int)mi.Tag! == sec; // 라디오처럼 하나만 체크
            };
            idleMenu.DropDownItems.Add(item);
        }

        var exitItem = new ToolStripMenuItem("종료");
        exitItem.Click += (_, _) => onExit();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_pauseItem);
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(idleMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _icon = new NotifyIcon
        {
            Icon = CreateGlyphIcon(string.Empty),
            Text = "CarrotIME — 입력 상태 표시기",
            Visible = true,
            ContextMenuStrip = menu
        };
    }

    private static string FormatSeconds(int sec) => sec % 60 == 0 ? $"{sec / 60}분" : $"{sec}초";

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

    // 32x32 기준 당근: 위에 초록 잎, 아래로 뾰족한 주황 몸통.
    private static void DrawCarrot(Graphics g)
    {
        using var leaf = new SolidBrush(CarrotLeaf);
        g.FillEllipse(leaf, 14, 0, 4, 9);   // 가운데 잎
        g.FillEllipse(leaf, 9, 2, 4, 8);    // 왼쪽 잎
        g.FillEllipse(leaf, 19, 2, 4, 8);   // 오른쪽 잎

        using var body = new SolidBrush(CarrotBody);
        var cone = new[]
        {
            new Point(6, 11), new Point(26, 11), new Point(16, 31)
        };
        g.FillPolygon(body, cone);
        g.FillEllipse(body, 6, 6, 20, 12);  // 둥근 윗부분
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
