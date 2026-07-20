using System.Drawing;
using System.Windows.Forms;
using ImeCaretIndicator.Adapters;
using ImeCaretIndicator.Core;
using ImeCaretIndicator.Ui;

namespace ImeCaretIndicator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var overlay = new OverlayForm();
        _ = overlay.Handle; // 표시 전 핸들 생성 강제

        // Ticket 01: 폴링 루프(100ms). 이벤트 기반 저CPU 추적은 Ticket 03에서 대체.
        using var timer = new System.Windows.Forms.Timer { Interval = 100 };
        timer.Tick += (_, _) => Tick(overlay);
        timer.Start();

        Application.Run();
    }

    // 셸: OS에서 스냅샷을 읽고(ReadSnapshot) → seam(Decide)이 결정 → 그 결정을 적용만 한다.
    // 표시/숨김 결정은 전부 Decider 안에 있다.
    private static void Tick(OverlayForm overlay)
    {
        InputSnapshot snapshot = ReadSnapshot(overlay.PreferredIndicatorSize);
        IndicatorView view = Decider.Decide(snapshot);

        if (view.Visible)
            overlay.Render(view.Label, view.Position);
        else
            overlay.HideIndicator();
    }

    // OS 어댑터를 모아 순수 스냅샷을 만든다. 포그라운드 창이 없으면 캐럿 없는 스냅샷을 낸다
    // (그 경우의 '숨김'은 Decide가 판단).
    private static InputSnapshot ReadSnapshot(Size indicatorSize)
    {
        IntPtr foreground = Win32.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
            return new InputSnapshot(null, Rectangle.Empty, indicatorSize, 0, 0);

        uint threadId = Win32.GetWindowThreadProcessId(foreground, out _);
        var (langId, conversionMode) = ImeStateReader.Read(foreground, threadId);
        Rectangle? caret = CaretLocator.TryGetCaretRect(threadId);
        Rectangle screen = Screen.FromPoint(caret?.Location ?? Cursor.Position).Bounds;

        return new InputSnapshot(caret, screen, indicatorSize, langId, conversionMode);
    }
}
