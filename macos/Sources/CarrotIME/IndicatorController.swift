import AppKit
import ApplicationServices
import CarrotIMECore

/// 명령형 셸의 오케스트레이터 — 어댑터로 사실을 모아 InputSnapshot을 만들고 Decider.decide →
/// OverlayWindow/StatusItem에 반영한다. 상태는 이벤트(입력 소스 알림), 캐럿은 타이머 폴링.
final class IndicatorController {
    private let overlay = OverlayWindow()
    private let inputSource = InputSourceReader()
    private let status: StatusItemController
    private var timer: Timer?

    private var lastCaret: CGRect?
    private var lastFocused: AXUIElement?
    private var lastActivity = Date()

    // 입력 후 다시 표시되기까지의 유휴 시간. 스펙 기본 20초.
    // (테스트로 빨리 보고 싶으면 이 값을 낮춘다. 설정 로딩은 후속 작업.)
    private let idleReappearMs = Decider.defaultIdleReappearMs

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
            lastFocused = nil
            lastCaret = nil
            return
        }

        if !elementsEqual(reading.focusedElement, lastFocused) {
            // 포커스 대상이 바뀌면 유휴 카운트를 포커스 시점부터 다시 시작
            // (스펙: 포커스만으로는 뜨지 않고, 그 시점부터 유휴를 센다).
            lastFocused = reading.focusedElement
            lastCaret = reading.caret
            lastActivity = Date()
        } else if let c = reading.caret, c != lastCaret {
            // 같은 대상에서 캐럿 이동 = 입력 활동 → 유휴 카운트 리셋(별도 키 훅 없이 활동 판정).
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

    // 같은 UI 요소를 가리키는지(포커스 변화 판정). AXUIElement는 CFEqual로 의미 비교된다.
    private func elementsEqual(_ a: AXUIElement?, _ b: AXUIElement?) -> Bool {
        switch (a, b) {
        case (nil, nil): return true
        case let (x?, y?): return CFEqual(x, y)
        default: return false
        }
    }
}
