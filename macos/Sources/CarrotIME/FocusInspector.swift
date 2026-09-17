import ApplicationServices

/// 편집 포커스 판정(엄격) — Windows FocusInspector의 macOS 대응.
/// 스펙: 진짜 편집 텍스트 컨트롤에만 반응하고 읽기 전용(브라우저 본문 등)은 제외한다.
///
/// 판정 순서(Windows판의 정신을 따름 — 거기선 ValuePattern readonly→거부,
/// TextPattern IsReadOnly bool 확정→따름, 불가면 role 화이트리스트):
///   1) 편집 텍스트 역할/서브역할이 아니면 거부 — 버튼·정적텍스트·웹영역 오탐 차단.
///   2) 값(AXValue)이 설정 불가면 읽기 전용 → 거부.
///   3) 설정 가능 여부를 알 수 없으면 역할 화이트리스트를 신뢰.
enum FocusInspector {
    private static let editableRoles: Set<String> = ["AXTextField", "AXTextArea", "AXComboBox"]
    private static let editableSubroles: Set<String> = ["AXSearchField"] // 검색창

    static func isEditable(_ elem: AXUIElement, role: String) -> Bool {
        let subroleOK = string(elem, "AXSubrole").map(editableSubroles.contains) ?? false
        guard editableRoles.contains(role) || subroleOK else { return false }

        switch valueSettable(elem) {
        case .some(true): return true
        case .some(false): return false // 읽기 전용
        case .none: return true          // 판정 불가 → 역할 신뢰
        }
    }

    // AXValue가 설정 가능한가. 속성 자체가 없으면 nil(판정 불가).
    private static func valueSettable(_ elem: AXUIElement) -> Bool? {
        var settable: DarwinBoolean = false
        guard AXUIElementIsAttributeSettable(elem, kAXValueAttribute as CFString, &settable) == .success else {
            return nil
        }
        return settable.boolValue
    }

    private static func string(_ e: AXUIElement, _ attr: String) -> String? {
        var v: CFTypeRef?
        guard AXUIElementCopyAttributeValue(e, attr as CFString, &v) == .success else { return nil }
        return v as? String
    }
}
