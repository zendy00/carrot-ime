using System.Windows.Automation;

namespace ImeCaretIndicator.Adapters;

/// <summary>
/// 현재 포커스된 요소가 텍스트 입력 컨트롤인지 판정한다 — "편집 포커스" 신호(Ticket 03).
/// 캐럿을 못 얻는 상황에서도 편집 포커스 여부를 알 수 있게 해 Ticket 04(고정 폴백)의 토대가 된다.
/// </summary>
internal static class FocusInspector
{
    public static bool IsTextControl()
    {
        try
        {
            AutomationElement? focused = AutomationElement.FocusedElement;
            if (focused is null)
                return false;

            if (focused.GetCurrentPropertyValue(AutomationElement.IsTextPatternAvailableProperty) is true)
                return true;

            ControlType ct = focused.Current.ControlType;
            return ct == ControlType.Edit || ct == ControlType.Document || ct == ControlType.ComboBox;
        }
        catch
        {
            return false;
        }
    }
}
