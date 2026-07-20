using System.Windows.Forms;
using ImeCaretIndicator.Adapters;
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

        AppSettings settings = AppSettings.Load();

        using var controller = new IndicatorController();
        controller.Enabled = settings.Enabled;
        controller.Start();

        using var tray = new TrayIcon(
            enabled: settings.Enabled,
            autoStart: AutoStart.IsEnabled(),
            onEnabledChanged: enabled =>
            {
                controller.Enabled = enabled;
                settings.Enabled = enabled;
                settings.Save();
            },
            onAutoStartChanged: autoStart =>
            {
                // 실제 적용 성공 여부를 확인해 설정·체크 상태를 현실과 맞춘다.
                bool applied = autoStart ? AutoStart.Enable() : !AutoStart.Disable();
                settings.AutoStart = applied;
                settings.Save();
                return applied;
            },
            onExit: Application.Exit);

        Application.Run();
    }
}
