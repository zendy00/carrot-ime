using System.Windows.Forms;
using CarrotIME.Adapters;
using CarrotIME.Ui;

namespace CarrotIME;

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
        controller.IdleSeconds = settings.IdleSeconds;
        controller.Enabled = settings.Enabled;

        using var tray = new TrayIcon(
            enabled: settings.Enabled,
            autoStart: AutoStart.IsEnabled(),
            idleSeconds: settings.IdleSeconds,
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
            onIdleSecondsChanged: seconds =>
            {
                controller.IdleSeconds = seconds;
                settings.IdleSeconds = seconds;
                settings.Save();
            },
            onExit: Application.Exit);

        controller.StateChanged = tray.SetGlyph; // 트레이 아이콘에 현재 입력 상태 글자 표시
        controller.Start();

        Application.Run();
    }
}
