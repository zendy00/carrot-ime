using System.Windows.Automation;

namespace CarrotIME.Adapters;

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

            // 진짜 편집 텍스트 컨트롤(Edit/Document)만 인정한다. 브라우저는 문서 조상에
            // TextPattern을 노출하는 경우가 많아, IsTextPatternAvailable만 보면 버튼·링크에
            // 포커스가 있어도 편집으로 오판된다 → 여기선 컨트롤 타입으로 엄격히 판정.
            ControlType ct = focused.Current.ControlType;
            if (ct != ControlType.Edit && ct != ControlType.Document)
                return false;

            // 읽기 전용 텍스트(표시 전용)면 입력 불가로 간주.
            if (focused.TryGetCurrentPattern(ValuePattern.Pattern, out object vp)
                && vp is ValuePattern value && value.Current.IsReadOnly)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}
