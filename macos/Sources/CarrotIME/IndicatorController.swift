import AppKit
import CarrotIMECore

/// 명령형 셸의 오케스트레이터 — 어댑터로 사실을 모아 InputSnapshot을 만들고 Decider.decide →
/// OverlayWindow/StatusItem에 반영한다. 상태는 이벤트(입력 소스 알림), 캐럿은 타이머 폴링.
final class IndicatorController {
    private let overlay = OverlayWindow()
    private let inputSource = InputSourceReader()
    private let status: StatusItemController
    private var timer: Timer?

    private var lastCaret: CGRect?
    private var lastActivity = Date()

    // 스켈레톤: 데모가 바로 보이도록 유휴 debounce를 끈다(0 = 편집 포커스면 항상 표시).
    // 실제 배포 시엔 Decider.defaultIdleReappearMs(20s)로. 설정 로딩은 후속 작업.
    private let idleReappearMs = 0

    init(status: StatusItemController) {
        self.status = status
        inputSource.onChange = { [weak self] id in
            guard let self else { return }
            self.status.update(state: Decider.resolveState(id), inputSourceId: id)
            self.tick()
        }
        status.update(state: Decider.resolveState(inputSource.currentId), inputSourceId: inputSource.currentId)
    }

    func start() {
        let t = Timer(timeInterval: 0.15, repeats: true) { [weak self] _ in self?.tick() }
        RunLoop.main.add(t, forMode: .common)
        timer = t
    }

    private func tick() {
        guard let reading = CaretReader.read() else {
            overlay.apply(.hidden)
            return
        }

        // 캐럿 이동 = 입력 활동 → 유휴 카운트 리셋(별도 키 훅 없이 활동 판정).
        if let c = reading.caret, c != lastCaret {
            lastActivity = Date()
            lastCaret = c
        }
        let idleMs = Int(Date().timeIntervalSince(lastActivity) * 1000)

        let anchor = reading.caret?.origin ?? reading.windowBounds.origin
        let snap = InputSnapshot(
            editableFocus: reading.editableFocus,
            caret: reading.caret,
            activeWindowBounds: reading.windowBounds,
            screenBounds: Coord.screenBoundsTopLeft(containing: anchor),
            indicatorSize: overlay.indicatorSize,
            inputSourceId: inputSource.currentId,
            millisSinceInputActivity: idleMs)

        overlay.apply(Decider.decide(snap, idleReappearMs: idleReappearMs))
    }
}
