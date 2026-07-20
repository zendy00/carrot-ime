using System.Windows.Forms;

namespace ImeCaretIndicator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var controller = new IndicatorController();
        controller.Start();

        Application.Run();
    }
}
