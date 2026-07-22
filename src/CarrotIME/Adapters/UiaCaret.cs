using System.Drawing;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Accessibility;

namespace CarrotIME.Adapters;

/// <summary>
/// UI Automation TextPattern의 선택 경계로 캐럿 위치를 얻는다. 폴백 체인의 두 번째 소스
/// (네이티브 캐럿이 없는 크롬·일렉트론 웹 입력폼 등을 커버). COM UIA 직접 호출(WPF 비의존).
/// </summary>
internal static class UiaCaret
{
    // threadId는 델리게이트 시그니처 통일용 — UIA는 전역 FocusedElement를 쓴다.
    public static Rectangle? TryGet(uint threadId)
    {
        try
        {
            IUIAutomationElement focused = UiaClient.Instance.GetFocusedElement();
            if (focused is null)
                return null;

            if (focused.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId)
                is not IUIAutomationTextPattern textPattern)
                return null;

            IUIAutomationTextRangeArray selection = textPattern.GetSelection();
            if (selection is null || selection.Length == 0)
                return null;

            return ReadFirstRect(selection.GetElement(0));
        }
        catch
        {
            // UIA 호출은 요소 소멸 등으로 COMException을 던질 수 있음 → 다음 소스로 폴백
            return null;
        }
    }

    // GetBoundingRectangles는 [x, y, width, height] 4개씩 묶인 double SAFEARRAY를 반환.
    // 소유권이 호출자에 있어 SafeArrayDestroy로 해제해야 한다.
    private static unsafe Rectangle? ReadFirstRect(IUIAutomationTextRange range)
    {
        SAFEARRAY* sa = range.GetBoundingRectangles();
        if (sa is null)
            return null;
        try
        {
            if (sa->cDims != 1 || sa->rgsabound[0].cElements < 4)
                return null;

            double* rects = (double*)sa->pvData;
            // 원시 사각형만 반환한다. "넓은 사각형은 실제 캐럿이 아니다"라는 판정·보정은
            // 순수 코어(Decider)에서 하도록 넘긴다(테스트 가능한 위치 결정 seam 유지).
            return new Rectangle(
                (int)rects[0],
                (int)rects[1],
                Math.Max(1, (int)rects[2]),
                Math.Max(1, (int)rects[3]));
        }
        finally
        {
            PInvoke.SafeArrayDestroy(sa);
        }
    }
}
