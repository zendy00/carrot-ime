import AppKit
import ApplicationServices

/// AX 어댑터 — 최상단 앱의 포커스 요소에서 편집 여부·캐럿 rect·창 경계를 읽는다.
/// (OS 사실 수집만; 판정은 Core.) 접근성 권한(TCC)이 없으면 대부분 nil을 반환한다.
struct CaretReading {
    var editableFocus: Bool
    var caret: CGRect?          // top-left 전역 좌표(정밀 캐럿, 없을 수 있음)
    var fieldFrame: CGRect?     // top-left 전역 좌표(포커스 요소 프레임, 캐럿 폴백 앵커)
    var windowBounds: CGRect    // top-left 전역 좌표
    var role: String
    var focusedElement: AXUIElement   // 이 판독의 대상(판독 캐시 키).
}

enum CaretReader {
    // AXManualAccessibility를 켠 앱 pid(중복 세팅 방지). Chromium/Electron은 이걸 켜야 AX 트리가 산다.
    private static var axEnabledPids: Set<pid_t> = []

    /// 최상단 앱의 포커스 요소만 얻는다(AX 질의 1회). 포커스 변화 감지용 — 전체 판독보다 훨씬 싸다.
    static func focusedElement(of app: NSRunningApplication) -> AXUIElement? {
        let axApp = AXUIElementCreateApplication(app.processIdentifier)
        enableChromiumAXIfNeeded(app.processIdentifier, axApp)
        guard let focused = copyAttr(axApp, kAXFocusedUIElementAttribute) else { return nil }
        return (focused as! AXUIElement)
    }

    /// 포커스 요소의 편집 여부·캐럿·프레임·창 경계를 판독한다.
    /// 편집 요소가 아니면 어차피 표시하지 않으므로 캐럿·프레임·창 질의는 생략한다.
    static func read(_ elem: AXUIElement, of app: NSRunningApplication) -> CaretReading {
        let role = (copyAttr(elem, kAXRoleAttribute) as? String) ?? ""
        guard FocusInspector.isEditable(elem, role: role) else {
            return CaretReading(editableFocus: false, caret: nil, fieldFrame: nil, windowBounds: .zero,
                                role: role, focusedElement: elem)
        }
        let axApp = AXUIElementCreateApplication(app.processIdentifier)
        return CaretReading(
            editableFocus: true,
            caret: caretRect(elem),
            fieldFrame: frameRect(elem),
            windowBounds: focusedWindowBounds(axApp) ?? .zero,
            role: role,
            focusedElement: elem)
    }

    // Chromium/Electron AX 트리 활성화(앱당 1회). WebKit/네이티브엔 무해.
    private static func enableChromiumAXIfNeeded(_ pid: pid_t, _ axApp: AXUIElement) {
        guard !axEnabledPids.contains(pid) else { return }
        AXUIElementSetAttributeValue(axApp, "AXManualAccessibility" as CFString, kCFBooleanTrue)
        axEnabledPids.insert(pid)
    }

    // 포커스 요소의 프레임(kAXPosition + kAXSize) — 정밀 캐럿 실패 시 폴백 앵커.
    private static func frameRect(_ elem: AXUIElement) -> CGRect? {
        guard let posV = copyAttr(elem, kAXPositionAttribute),
              let sizeV = copyAttr(elem, kAXSizeAttribute) else { return nil }
        var pos = CGPoint.zero
        var size = CGSize.zero
        AXValueGetValue(posV as! AXValue, .cgPoint, &pos)
        AXValueGetValue(sizeV as! AXValue, .cgSize, &size)
        return CGRect(origin: pos, size: size)
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
