import AppKit
import ApplicationServices

setvbuf(stdout, nil, _IONBF, 0) // 파이프·백그라운드에서도 즉시 보이도록 무버퍼링.

// AX 캐럿 rect 프로브 — Windows의 GetGUIThreadInfo/UIA/MSAA 폴백 체인 대응물이
// macOS에서 실제로 캐럿 사각형을 주는지 실측한다. 접근성 권한(TCC)이 필수다.
//
// 30초간 0.5s마다 "현재 최상단 앱의 포커스된 요소"의 캐럿 rect를 찍는다.
// 실행한 뒤 TextEdit·메모·Chrome·터미널 등을 클릭해 타이핑하며 rect가 따라오는지 본다.
//
// 확인할 것:
//   - 어떤 앱이 rect를 주고 어떤 앱이 <none>인가(폴백 정책 근거).
//   - 빈 입력란/선택 영역에서 rect 폭이 어떻게 나오나(Decider의 MaxCaretWidth 판정 대응).

func copyAttr(_ e: AXUIElement, _ attr: String) -> CFTypeRef? {
    var v: CFTypeRef?
    return AXUIElementCopyAttributeValue(e, attr as CFString, &v) == .success ? v : nil
}

func axRole(_ e: AXUIElement) -> String {
    (copyAttr(e, kAXRoleAttribute as String) as? String) ?? "?"
}

func focusedElement(of pid: pid_t) -> AXUIElement? {
    let app = AXUIElementCreateApplication(pid)
    guard let f = copyAttr(app, kAXFocusedUIElementAttribute as String) else { return nil }
    return (f as! AXUIElement)
}

// 캐럿(빈 선택)의 화면 rect. 선택 영역이면 그 끝을 나타낸다.
func caretRect(_ elem: AXUIElement) -> CGRect? {
    guard let rv = copyAttr(elem, kAXSelectedTextRangeAttribute as String) else { return nil }
    let axRange = rv as! AXValue
    guard let bv = boundsForRange(elem, axRange) else { return nil }
    var rect = CGRect.zero
    return AXValueGetValue(bv, .cgRect, &rect) ? rect : nil
}

func boundsForRange(_ elem: AXUIElement, _ range: AXValue) -> AXValue? {
    var out: CFTypeRef?
    let err = AXUIElementCopyParameterizedAttributeValue(
        elem, kAXBoundsForRangeParameterizedAttribute as CFString, range, &out)
    guard err == .success, let o = out else { return nil }
    return (o as! AXValue)
}

let promptKey = kAXTrustedCheckOptionPrompt.takeUnretainedValue()
let opts = [promptKey: true] as CFDictionary
let trusted = AXIsProcessTrustedWithOptions(opts)
print("AX trusted:", trusted)
if !trusted {
    print("→ 접근성 권한 필요: 시스템 설정 > 개인정보 보호 및 보안 > 손쉬운 사용에서")
    print("  이 프로세스를 호스팅하는 터미널 앱을 허용한 뒤 다시 실행하세요.\n")
}
print("30초간 0.5s마다 최상단 앱의 캐럿을 읽습니다. TextEdit 등을 클릭해 타이핑해 보세요.\n")

let start = Date()
while Date().timeIntervalSince(start) < 30 {
    if let app = NSWorkspace.shared.frontmostApplication {
        let name = app.localizedName ?? "?"
        if let elem = focusedElement(of: app.processIdentifier) {
            let role = axRole(elem)
            if let r = caretRect(elem) {
                print(String(format: "%-16@ [%@] caret=(%.0f,%.0f  %.0fx%.0f)",
                             name as NSString, role, r.origin.x, r.origin.y, r.size.width, r.size.height))
            } else {
                print("\(name) [\(role)] caret=<none>")
            }
        } else {
            print("\(name)  focused=<none>")
        }
    }
    Thread.sleep(forTimeInterval: 0.5)
}
print("\n완료.")
