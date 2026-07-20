using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImeCaretIndicator.Adapters;

namespace ImeCaretIndicator.Ui;

/// <summary>
/// 시스템 트레이 아이콘 + 메뉴(일시정지/재개, Windows 시작 시 실행, 종료).
/// 상태 변경은 콜백으로 밖에 알린다(설정 저장·컨트롤러 제어는 호출부가 담당).
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private IntPtr _hIcon;

    public TrayIcon(
        bool enabled, bool autoStart,
        Action<bool> onEnabledChanged, Func<bool, bool> onAutoStartChanged, Action onExit)
    {
        // 체크 = 일시정지 상태(=꺼짐). CheckOnClick으로 Click 전에 Checked가 갱신됨.
        _pauseItem = new ToolStripMenuItem("일시정지") { Checked = !enabled, CheckOnClick = true };
        _pauseItem.Click += (_, _) => onEnabledChanged(!_pauseItem.Checked);

        _autoStartItem = new ToolStripMenuItem("Windows 시작 시 실행") { Checked = autoStart, CheckOnClick = true };
        // 콜백이 실제 적용 결과(성공 여부)를 돌려주면 체크를 현실과 맞춘다.
        _autoStartItem.Click += (_, _) => _autoStartItem.Checked = onAutoStartChanged(_autoStartItem.Checked);

        var exitItem = new ToolStripMenuItem("종료");
        exitItem.Click += (_, _) => onExit();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_pauseItem);
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _icon = new NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "IME 상태 표시기",
            Visible = true,
            ContextMenuStrip = menu
        };
    }

    // 트레이용 파란 원형 아이콘을 런타임 생성(별도 .ico 리소스 불필요).
    private Icon CreateIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(0, 122, 255));
            g.FillEllipse(brush, 1, 1, 14, 14);
        }
        _hIcon = bmp.GetHicon();
        return Icon.FromHandle(_hIcon);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        if (_hIcon != IntPtr.Zero)
            Win32.DestroyIcon(_hIcon); // GetHicon으로 만든 핸들 정리
    }
}
