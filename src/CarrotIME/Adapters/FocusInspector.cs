using Windows.Win32.UI.Accessibility;

namespace CarrotIME.Adapters;

/// <summary>
/// 현재 포커스된 요소가 텍스트 입력 컨트롤인지 판정한다 — "편집 포커스" 신호(Ticket 03).
/// 캐럿을 못 얻는 상황에서도 편집 포커스 여부를 알 수 있게 해 Ticket 04(고정 폴백)의 토대가 된다.
/// COM UIA 직접 호출(WPF 비의존).
/// </summary>
internal static class FocusInspector
{
    public static bool IsTextControl()
    {
        try
        {
            IUIAutomationElement focused = UiaClient.Instance.GetFocusedElement();
            if (focused is null)
                return false;

            // 읽기 전용 값 컨트롤(표시 전용)이면 입력 불가로 간주.
            if (focused.GetCurrentPattern(UIA_PATTERN_ID.UIA_ValuePatternId)
                is IUIAutomationValuePattern value && value.CurrentIsReadOnly)
                return false;

            // TextPattern의 읽기 전용 속성이 확정 답(bool)을 주면 컨트롤 타입과 무관하게
            // 그걸 따른다 — Gmail 받는사람 같은 role=combobox 입력, role 없는 contenteditable도
            // 편집으로 잡히고, 브라우저 본문·PDF는 읽기 전용 true라 제외된다.
            if (focused.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId)
                is IUIAutomationTextPattern text
                && text.DocumentRange.GetAttributeValue(
                    UIA_TEXTATTRIBUTE_ID.UIA_IsReadOnlyAttributeId) is bool readOnly)
                return !readOnly;

            // 판정 불가(TextPattern 없음·혼합 속성) → 진짜 편집 텍스트 컨트롤 타입만 인정.
            UIA_CONTROLTYPE_ID ct = focused.CurrentControlType;
            return ct == UIA_CONTROLTYPE_ID.UIA_EditControlTypeId
                || ct == UIA_CONTROLTYPE_ID.UIA_DocumentControlTypeId;
        }
        catch
        {
            return false;
        }
    }
}
