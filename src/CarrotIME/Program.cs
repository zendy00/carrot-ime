using System.Drawing;
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

        Color indicatorColor = Color.FromArgb(settings.IndicatorColorArgb);
        Color textColor = Color.FromArgb(settings.IndicatorTextColorArgb);

        using var controller = new IndicatorController();
        controller.IdleSeconds = settings.IdleSeconds;
        controller.IndicatorColor = indicatorColor;
        controller.IndicatorTextColor = textColor;
        controller.Enabled = settings.Enabled;

        using var tray = new TrayIcon(
            enabled: settings.Enabled,
            autoStart: AutoStart.IsEnabled(),
            idleSeconds: settings.IdleSeconds,
            indicatorColor: indicatorColor,
            textColor: textColor,
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
            onColorChanged: color =>
            {
                controller.IndicatorColor = color;
                settings.IndicatorColorArgb = color.ToArgb();
                settings.Save();
            },
            onTextColorChanged: color =>
            {
                controller.IndicatorTextColor = color;
                settings.IndicatorTextColorArgb = color.ToArgb();
                settings.Save();
            },
            onExit: Application.Exit);

        controller.StateChanged = tray.SetGlyph; // 트레이 아이콘에 현재 입력 상태 글자 표시
        controller.Start();

        // 시작 초기화가 끝난 시점에 작업 집합을 한 번 반납(시작 직후 RAM 점유 축소).
        Adapters.Win32.TrimWorkingSet();

        Application.Run();
    }
}
