using System.Runtime.InteropServices;

namespace CarrotIME.Adapters;

/// <summary>
/// 마지막 키/마우스 입력 이후 경과 밀리초. GetLastInputInfo 로컬 호출 하나가 전부다.
///
/// 예전에는 "캐럿이 움직였나"로 입력을 감지했는데, 그러려면 매 틱 캐럿을 해석해야 했고
/// 네이티브 캐럿이 없는 앱(Chromium/WebView2 기반 터미널·브라우저)에서는 그게 곧
/// 크로스 프로세스 UIA 질의였다 — 대상 앱의 UI 스레드를 동기로 붙잡아 입력이 씹혔다.
/// 부수적으로, 캐럿을 못 얻어 입력 활동을 아예 감지하지 못하던 앱들의 한계도 사라진다.
/// </summary>
internal static class InputActivity
{
    /// <summary>
    /// 마지막 입력의 시각 도장(32비트 GetTickCount 기준). 입력이 있을 때마다 값이 바뀌므로
    /// "지난번 이후 새 입력이 있었나"를 이 값의 변화로 정확히 알 수 있다 — 캐럿은 입력이
    /// 있을 때만 움직이니, 캐럿을 다시 읽어야 하는지 판단하는 근거가 된다.
    /// </summary>
    public static uint LastInputTick()
    {
        var info = new Win32.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<Win32.LASTINPUTINFO>() };
        if (!Win32.GetLastInputInfo(ref info))
            return unchecked((uint)Environment.TickCount); // 못 읽으면 '방금 입력함'으로 보수적 처리
        return info.dwTime;
    }

    /// <summary>주어진 입력 시각 도장 이후 경과 밀리초.</summary>
    // dwTime은 32비트라 약 49.7일마다 래핑한다. 부호 없는 뺄셈이면 래핑 구간에서도 값이 맞는다.
    public static long MillisSince(uint inputTick)
        => unchecked((uint)Environment.TickCount - inputTick);
}
