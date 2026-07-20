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

    private static void Tick(OverlayForm overlay)
    {
        IntPtr foreground = Win32.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            overlay.HideIndicator();
            return;
        }

        uint threadId = Win32.GetWindowThreadProcessId(foreground, out _);
        var (langId, conversionMode) = ImeStateReader.Read(foreground, threadId);
        Rectangle? caret = CaretLocator.TryGetCaretRect(threadId);

        Rectangle screen = Screen.FromPoint(caret?.Location ?? Cursor.Position).Bounds;

        var snapshot = new InputSnapshot(
            Caret: caret,
            ScreenBounds: screen,
            IndicatorSize: overlay.PreferredIndicatorSize,
            KeyboardLangId: langId,
            ConversionMode: conversionMode);

        IndicatorView view = Decider.Decide(snapshot);
        if (view.Visible)
            overlay.Render(view.Label, view.Position);
        else
            overlay.HideIndicator();
    }
}
