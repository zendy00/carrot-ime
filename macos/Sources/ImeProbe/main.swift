import Carbon
import Foundation

setvbuf(stdout, nil, _IONBF, 0) // 파이프·백그라운드에서도 즉시 보이도록 무버퍼링.

// 한/영·입력 소스 전환을 이벤트로 관찰하는 프로브.
// macOS는 조합모드 폴링(Windows) 대신 kTISNotifySelectedKeyboardInputSourceChanged
// 분산 알림을 쏜다 — 상태 변화가 이벤트 기반임을 실측한다.
// 확인할 것:
//   1) ABC ↔ 2-Set Korean 전환 시 알림 + ID 변화가 오는가.
//   2) 2-Set Korean 안에서 한/영 토글(Caps Lock 등) 시에도 별도 신호가 오는가,
//      아니면 한글 입력 소스 하나로만 보이는가(= 미검증 항목).

func str(_ src: TISInputSource, _ key: CFString) -> String {
    guard let p = TISGetInputSourceProperty(src, key) else { return "<nil>" }
    return Unmanaged<CFString>.fromOpaque(p).takeUnretainedValue() as String
}

func classify(_ id: String) -> String {
    if id.contains("inputmethod.Korean") { return "한  (Hangul)" }
    if id.hasPrefix("com.apple.keylayout.") { return "A   (Latin layout)" }
    if id.contains("Japanese") { return "あ  (Japanese)" }
    if id.contains(".SCIM") || id.contains(".TCIM") || id.lowercased().contains("chinese") { return "中  (Chinese)" }
    return "IME (\(id))"
}

func report(_ tag: String) {
    guard let cf = TISCopyCurrentKeyboardInputSource()?.takeRetainedValue() else {
        print("[\(tag)] <no current input source>")
        return
    }
    let id = str(cf, kTISPropertyInputSourceID)
    let name = str(cf, kTISPropertyLocalizedName)
    print("[\(tag)] \(classify(id))   name=\(name)   id=\(id)")
}

// C 함수 포인터로 넘길 콜백 — 캡처 없는 전역 클로저여야 한다.
let callback: CFNotificationCallback = { _, _, _, _, _ in
    report("change")
}

report("initial")
let center = CFNotificationCenterGetDistributedCenter()
CFNotificationCenterAddObserver(
    center, nil, callback,
    kTISNotifySelectedKeyboardInputSourceChanged, nil, .deliverImmediately)

print("\n입력 소스를 바꾸거나(한/영 키·Caps Lock) 관찰하세요. Ctrl+C 로 종료.\n")
CFRunLoopRun()
