import AppKit
import Carbon

/// 입력 소스 어댑터 — 현재 소스 ID를 읽고, 전환 알림을 이벤트로 구독한다.
/// macOS는 상태 폴링이 불필요하다(Windows와의 차이). 단 알림이 중복 발사되므로 동일 ID는 dedupe.
final class InputSourceReader {
    private(set) var currentId: String = InputSourceReader.readId()
    var onChange: ((String) -> Void)?

    init() {
        let observer = Unmanaged.passUnretained(self).toOpaque()
        CFNotificationCenterAddObserver(
            CFNotificationCenterGetDistributedCenter(),
            observer,
            { _, obs, _, _, _ in
                guard let obs = obs else { return }
                Unmanaged<InputSourceReader>.fromOpaque(obs).takeUnretainedValue().refresh()
            },
            kTISNotifySelectedKeyboardInputSourceChanged,
            nil,
            .deliverImmediately)
    }

    private func refresh() {
        let id = Self.readId()
        guard id != currentId else { return } // 중복 알림 무시
        currentId = id
        onChange?(id)
    }

    static func readId() -> String {
        guard let cf = TISCopyCurrentKeyboardInputSource()?.takeRetainedValue(),
              let p = TISGetInputSourceProperty(cf, kTISPropertyInputSourceID) else { return "" }
        return Unmanaged<CFString>.fromOpaque(p).takeUnretainedValue() as String
    }
}
