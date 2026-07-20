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

    /// <summary>자동 시작을 등록한다. 성공하면 true.</summary>
    public static bool Enable()
    {
        string exe = Environment.ProcessPath ?? "";
        // `dotnet run`으로 실행하면 ProcessPath가 앱 exe가 아니라 dotnet 호스트를 가리키므로
        // 잘못된 대상 등록을 피한다. 게시된 apphost(.exe)에서만 등록.
        if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return false;
        return Run($"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\"\" /SC ONLOGON /RL HIGHEST /F") == 0;
    }

    /// <summary>자동 시작 등록을 해제한다. 성공하면 true.</summary>
    public static bool Disable() => Run($"/Delete /TN \"{TaskName}\" /F") == 0;

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
            // 파이프를 끝까지 읽어 버퍼가 차서 자식이 막히는 것을 방지.
            p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit(5000);
            return p.HasExited ? p.ExitCode : -1;
        }
        catch
        {
            return -1;
        }
    }
}
