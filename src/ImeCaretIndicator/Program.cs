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
                if (autoStart)
                    AutoStart.Enable();
                else
                    AutoStart.Disable();
                settings.AutoStart = autoStart;
                settings.Save();
            },
            onExit: Application.Exit);

        Application.Run();
    }
}
