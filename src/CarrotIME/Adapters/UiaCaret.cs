using System.Drawing;
using System.Windows.Automation;

namespace CarrotIME.Adapters;

/// <summary>
/// UI Automation TextPattern의 선택 경계로 캐럿 위치를 얻는다. 폴백 체인의 두 번째 소스
/// (네이티브 캐럿이 없는 크롬·일렉트론 웹 입력폼 등을 커버).
/// </summary>
internal static class UiaCaret
{
    // threadId는 델리게이트 시그니처 통일용 — UIA는 전역 FocusedElement를 쓴다.
    public static Rectangle? TryGet(uint threadId)
    {
        try
        {
            AutomationElement? focused = AutomationElement.FocusedElement;
            if (focused is null)
                return null;

            if (!focused.TryGetCurrentPattern(TextPattern.Pattern, out object patternObj)
                || patternObj is not TextPattern textPattern)
                return null;

            var selection = textPattern.GetSelection();
            if (selection.Length == 0)
                return null;

            var rects = selection[0].GetBoundingRectangles();
            if (rects.Length == 0)
                return null;

            // 원시 사각형만 반환한다. "넓은 사각형은 실제 캐럿이 아니다"라는 판정·보정은
            // 순수 코어(Decider)에서 하도록 넘긴다(테스트 가능한 위치 결정 seam 유지).
            System.Windows.Rect r = rects[0]; // 화면 좌표(double)
            return new Rectangle(
                (int)r.X,
                (int)r.Y,
                Math.Max(1, (int)r.Width),
                Math.Max(1, (int)r.Height));
        }
        catch
        {
            // UIA 호출은 ElementNotAvailableException 등을 던질 수 있음 → 다음 소스로 폴백
            return null;
        }
    }
}
