using System.Drawing;
using System.Windows.Automation;

namespace ImeCaretIndicator.Adapters;

/// <summary>
/// UI Automation TextPattern의 선택 경계로 캐럿 위치를 얻는다. 폴백 체인의 두 번째 소스
/// (네이티브 캐럿이 없는 크롬·일렉트론 웹 입력폼 등을 커버).
/// </summary>
internal static class UiaCaret
{
    // 이보다 넓은 사각형은 얇은 텍스트 캐럿이 아니라 입력창/선택 영역으로 본다(px).
    private const int CaretWidthThreshold = 6;

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

            System.Windows.Rect r = rects[0]; // 화면 좌표(double)
            int width = Math.Max(1, (int)r.Width);
            int height = Math.Max(1, (int)r.Height);

            // 얇은 캐럿(폭 몇 px)이면 그대로. 넓으면(사이트에 따라 입력창 박스/선택 영역을
            // 통째로 주는 경우 — 예: 일부 웹 검색창) 실제 캐럿이 아니므로, 오른쪽 끝이 아니라
            // 시작(왼쪽)을 캐럿으로 간주해 인디케이터가 박스 바깥으로 나가지 않게 한다.
            if (width > CaretWidthThreshold)
                width = 1;

            return new Rectangle((int)r.X, (int)r.Y, width, height);
        }
        catch
        {
            // UIA 호출은 ElementNotAvailableException 등을 던질 수 있음 → 다음 소스로 폴백
            return null;
        }
    }
}
