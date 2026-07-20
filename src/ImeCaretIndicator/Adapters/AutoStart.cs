using System.Diagnostics;

namespace ImeCaretIndicator.Adapters;

/// <summary>
/// 작업 스케줄러로 로그온 자동 시작을 등록/해제한다. 앱이 관리자 권한이라
/// 레지스트리 Run 키로는 상승 실행이 안 되므로, "가장 높은 권한으로 실행"(/RL HIGHEST)
/// 로그온 작업(/SC ONLOGON)으로 등록해 부팅 시 UAC 없이 상승 실행한다(ADR-0003).
/// </summary>
internal static class AutoStart
{
    private const string TaskName = "ImeCaretIndicator";

    public static bool IsEnabled() => Run($"/Query /TN \"{TaskName}\"") == 0;

    public static void Enable()
    {
        string exe = Environment.ProcessPath ?? "";
        if (exe.Length == 0)
            return;
        Run($"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\"\" /SC ONLOGON /RL HIGHEST /F");
    }

    public static void Disable() => Run($"/Delete /TN \"{TaskName}\" /F");

    private static int Run(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks.exe", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using Process? p = Process.Start(psi);
            if (p is null)
                return -1;
            p.WaitForExit(5000);
            return p.HasExited ? p.ExitCode : -1;
        }
        catch
        {
            return -1;
        }
    }
}
