import AppKit
import ApplicationServices

// 브라우저 내부 input 캐럿 조사 프로브.
// 관건: WebKit(Safari)은 AX 캐럿을 비교적 노출하지만, Chromium(Chrome)·Electron은
// AX 트리가 기본 비활성이라 AXManualAccessibility=YES 를 앱 요소에 세팅해야 켜진다.
// 이 프로브는 최상단 앱에 그 플래그를 세팅한 뒤 포커스 요소의 역할·서브역할·조상체인·캐럿 rect를 찍는다.

setvbuf(stdout, nil, _IONBF, 0)

func copyAttr(_ e: AXUIElement, _ attr: String) -> CFTypeRef? {
    var v: CFTypeRef?
    return AXUIElementCopyAttributeValue(e, attr as CFString, &v) == .success ? v : nil
}
func str(_ e: AXUIElement, _ attr: String) -> String { (copyAttr(e, attr) as? String) ?? "-" }

func caretRect(_ e: AXUIElement) -> CGRect? {
    guard let rv = copyAttr(e, kAXSelectedTextRangeAttribute as String) else { return nil }
    var out: CFTypeRef?
    guard AXUIElementCopyParameterizedAttributeValue(
        e, kAXBoundsForRangeParameterizedAttribute as CFString, rv as! AXValue, &out) == .success,
        let o = out else { return nil }
    var r = CGRect.zero
    return AXValueGetValue(o as! AXValue, .cgRect, &r) ? r : nil
}

// 조상 역할 체인(AX 트리 구조 파악용 — AXWebArea 등이 보이면 웹 컨텐츠).
func ancestry(_ e: AXUIElement, limit: Int = 7) -> String {
    var chain: [String] = []
    var cur: AXUIElement? = e
    var i = 0
    while let c = cur, i < limit {
        let role = str(c, kAXRoleAttribute as String)
        let sub = copyAttr(c, kAXSubroleAttribute as String) as? String
        chain.append(sub.map { "\(role):\($0)" } ?? role)
        cur = copyAttr(c, kAXParentAttribute as String).map { $0 as! AXUIElement }
        i += 1
    }
    return chain.joined(separator: " < ")
}

let opts = [kAXTrustedCheckOptionPrompt.takeUnretainedValue(): true] as CFDictionary
print("AX trusted:", AXIsProcessTrustedWithOptions(opts), "\n")
print("40초간 0.5s마다 최상단 앱의 포커스 요소를 조사합니다.")
print("Safari·Chrome·Teams 등에서 input/textarea/contenteditable에 커서를 두세요.")
print("(Chromium은 첫 세팅 후 트리 구성에 잠깐 걸릴 수 있음 — 커서 둔 채 몇 초 기다려 보세요.)\n")

var enabled = Set<pid_t>()
let start = Date()
while Date().timeIntervalSince(start) < 40 {
    if let app = NSWorkspace.shared.frontmostApplication {
        let pid = app.processIdentifier
        let axApp = AXUIElementCreateApplication(pid)

        // Chromium/Electron/WebKit 완전 AX 활성화 시도(한 번).
        if !enabled.contains(pid) {
            AXUIElementSetAttributeValue(axApp, "AXManualAccessibility" as CFString, kCFBooleanTrue)
            AXUIElementSetAttributeValue(axApp, "AXEnhancedUserInterface" as CFString, kCFBooleanTrue)
            enabled.insert(pid)
        }

        let name = app.localizedName ?? "?"
        if let f = copyAttr(axApp, kAXFocusedUIElementAttribute as String) {
            let elem = f as! AXUIElement
            let role = str(elem, kAXRoleAttribute as String)
            let sub = str(elem, kAXSubroleAttribute as String)
            let caret = caretRect(elem).map {
                String(format: "(%.0f,%.0f %.0fx%.0f)", $0.origin.x, $0.origin.y, $0.width, $0.height)
            } ?? "<none>"
            print("\(name) | \(role)/\(sub) | caret=\(caret)")
            print("   ancestry: \(ancestry(elem))")
        } else {
            print("\(name) | focused=<none>")
        }
    }
    Thread.sleep(forTimeInterval: 0.5)
}
print("\n완료.")
