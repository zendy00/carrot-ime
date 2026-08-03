import AppKit
import ApplicationServices

/// AX 어댑터 — 최상단 앱의 포커스 요소에서 편집 여부·캐럿 rect·창 경계를 읽는다.
/// (OS 사실 수집만; 판정은 Core.) 접근성 권한(TCC)이 없으면 대부분 nil을 반환한다.
struct CaretReading {
    var editableFocus: Bool
    var caret: CGRect?          // top-left 전역 좌표
    var windowBounds: CGRect    // top-left 전역 좌표
    var role: String
}

enum CaretReader {
    static func read() -> CaretReading? {
        guard let app = NSWorkspace.shared.frontmostApplication else { return nil }
        let axApp = AXUIElementCreateApplication(app.processIdentifier)
        guard let focused = copyAttr(axApp, kAXFocusedUIElementAttribute) else { return nil }
        let elem = focused as! AXUIElement
        let role = (copyAttr(elem, kAXRoleAttribute) as? String) ?? ""
        return CaretReading(
            editableFocus: isEditable(elem, role: role),
            caret: caretRect(elem),
            windowBounds: focusedWindowBounds(axApp) ?? .zero,
            role: role)
    }

    // 스켈레톤 휴리스틱: 텍스트류 역할이거나 값 설정 가능(읽기 전용 배제 근사).
    // TODO: Windows FocusInspector 이식 — ValuePattern readonly / role 화이트리스트 정교화.
    private static func isEditable(_ elem: AXUIElement, role: String) -> Bool {
        let textRoles: Set<String> = ["AXTextField", "AXTextArea", "AXComboBox"]
        if textRoles.contains(role) { return true }
        var settable: DarwinBoolean = false
        if AXUIElementIsAttributeSettable(elem, kAXValueAttribute as CFString, &settable) == .success {
            return settable.boolValue
        }
        return false
    }

    // 캐럿(빈 선택)의 화면 rect. 선택 영역이면 그 범위 rect.
    private static func caretRect(_ elem: AXUIElement) -> CGRect? {
        guard let rv = copyAttr(elem, kAXSelectedTextRangeAttribute) else { return nil }
        let range = rv as! AXValue
        var out: CFTypeRef?
        let err = AXUIElementCopyParameterizedAttributeValue(
            elem, kAXBoundsForRangeParameterizedAttribute as CFString, range, &out)
        guard err == .success, let o = out else { return nil }
        var rect = CGRect.zero
        return AXValueGetValue(o as! AXValue, .cgRect, &rect) ? rect : nil
    }

    private static func focusedWindowBounds(_ axApp: AXUIElement) -> CGRect? {
        guard let w = copyAttr(axApp, kAXFocusedWindowAttribute) else { return nil }
        let win = w as! AXUIElement
        guard let posV = copyAttr(win, kAXPositionAttribute),
              let sizeV = copyAttr(win, kAXSizeAttribute) else { return nil }
        var pos = CGPoint.zero
        var size = CGSize.zero
        AXValueGetValue(posV as! AXValue, .cgPoint, &pos)
        AXValueGetValue(sizeV as! AXValue, .cgSize, &size)
        return CGRect(origin: pos, size: size)
    }

    private static func copyAttr(_ e: AXUIElement, _ attr: String) -> CFTypeRef? {
        var v: CFTypeRef?
        return AXUIElementCopyAttributeValue(e, attr as CFString, &v) == .success ? v : nil
    }
}
