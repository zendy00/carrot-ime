using Windows.Win32.UI.Accessibility;

namespace CarrotIME.Adapters;

/// <summary>
/// COM UIA(IUIAutomation) 클라이언트 싱글턴. WPF 관리형 UIA(System.Windows.Automation) 대신
/// COM을 직접 써서 WPF 스택 로드를 없앤다(메모리·exe 크기 절감).
/// </summary>
internal static class UiaClient
{
    // 생성 비용이 커서 앱 수명 동안 캐시 — 입력 중 120ms 폴링에서 재사용된다.
    private static IUIAutomation? _instance;

    public static IUIAutomation Instance => _instance ??= (IUIAutomation)new CUIAutomation();
}
