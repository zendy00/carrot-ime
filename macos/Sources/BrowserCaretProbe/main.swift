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

func ownerName(_ e: AXUIElement) -> String {
    var pid: pid_t = 0
    guard AXUIElementGetPid(e, &pid) == .success else { return "?" }
    return NSRunningApplication(processIdentifier: pid)?.localizedName ?? "pid \(pid)"
}

// 요소 하나를 한 줄로 요약(역할·서브역할·캐럿·소유앱·조상체인).
func describe(_ e: AXUIElement) -> String {
    let role = str(e, kAXRoleAttribute as String)
    let sub = str(e, kAXSubroleAttribute as String)
    let caret = caretRect(e).map {
        String(format: "(%.0f,%.0f %.0fx%.0f)", $0.origin.x, $0.origin.y, $0.width, $0.height)
    } ?? "<none>"
    return "\(role)/\(sub) caret=\(caret) owner=\(ownerName(e))\n      anc: \(ancestry(e))"
}

let opts = [kAXTrustedCheckOptionPrompt.takeUnretainedValue(): true] as CFDictionary
print("AX trusted:", AXIsProcessTrustedWithOptions(opts), "\n")
print("40초간 0.5s마다 (1) 최상단 앱 포커스 요소, (2) 시스템 전역 포커스 요소를 조사합니다.")
print("테스트: ① 외부 Safari/Chrome로 전환(Cmd-Tab) 후 input에 커서, ② orchterm 인앱 브라우저 input에 커서.")
print("(Chromium은 AX 활성화 후 트리 구성에 몇 초 걸릴 수 있음.)\n")

let systemWide = AXUIElementCreateSystemWide()
var enabled = Set<pid_t>()
var lastPrinted = ""
let start = Date()
while Date().timeIntervalSince(start) < 40 {
    let frontName = NSWorkspace.shared.frontmostApplication?.localizedName ?? "?"

    // (1) 최상단 앱 경로 — Chromium/WebKit AX 활성화 시도 후 포커스 요소.
    var appLine = "<no frontmost>"
    if let app = NSWorkspace.shared.frontmostApplication {
        let axApp = AXUIElementCreateApplication(app.processIdentifier)
        if !enabled.contains(app.processIdentifier) {
            AXUIElementSetAttributeValue(axApp, "AXManualAccessibility" as CFString, kCFBooleanTrue)
            AXUIElementSetAttributeValue(axApp, "AXEnhancedUserInterface" as CFString, kCFBooleanTrue)
            enabled.insert(app.processIdentifier)
        }
        appLine = copyAttr(axApp, kAXFocusedUIElementAttribute as String)
            .map { describe($0 as! AXUIElement) } ?? "<none>"
    }

    // (2) 시스템 전역 포커스 요소 — 프로세스 경계를 넘어 포커스 요소를 잡는다.
    let sysLine = copyAttr(systemWide, kAXFocusedUIElementAttribute as String)
        .map { describe($0 as! AXUIElement) } ?? "<none>"

    // 변화가 있을 때만 출력(0.5s 반복 소음 제거).
    let block = "front=\(frontName)\n  app: \(appLine)\n  sys: \(sysLine)"
    if block != lastPrinted {
        print(block)
        lastPrinted = block
    }
    Thread.sleep(forTimeInterval: 0.5)
}
print("\n완료.")
