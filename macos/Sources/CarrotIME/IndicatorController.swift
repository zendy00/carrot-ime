import AppKit
import ApplicationServices
import CarrotIMECore

/// 명령형 셸의 오케스트레이터 — 어댑터로 사실을 모아 InputSnapshot을 만들고 Decider.decide →
/// OverlayWindow/StatusItem에 반영한다. 상태는 이벤트(입력 소스 알림), 입력 활동은 타이머 폴링(로컬),
/// AX(캐럿·포커스)는 유휴일 때만 묻는다.
final class IndicatorController {
    private let overlay = OverlayWindow()
    private let inputSource = InputSourceReader()
    private let status: StatusItemController
    private var timer: Timer?

    // 포커스 변화 추적 — 앱이나 포커스 요소가 바뀌면 그 시점부터 유휴를 다시 센다
    // (스펙: 포커스만으로는 뜨지 않고, 그 시점부터 유휴를 센다).
    private var lastFrontPid: pid_t = 0
    private var lastFocusChangeAt = ProcessInfo.processInfo.systemUptime
    // 직전 틱이 유휴 구간이었나(= 포커스 요소를 확인했나). 아래 포커스 변화 판정 참고.
    private var checkedFocusLastTick = false

    // AX 판독 캐시. 편집 여부·캐럿 위치는 포커스 요소가 같고 그 사이 입력이 없으면 변하지 않는다
    // (캐럿은 입력으로만 움직인다). 그래서 유휴 중에도 포커스 요소 비교(AX 1회)만 하고 재판독은 건너뛴다.
    private var cachedReading: CaretReading?
    private var cachedAt: TimeInterval = 0
    private var lastLogAt = Date.distantPast

    private let settings = AppSettings.shared

    init(status: StatusItemController) {
        self.status = status
        inputSource.onChange = { [weak self] id in
            guard let self else { return }
            self.status.update(state: Decider.resolveState(id), inputSourceId: id)
            self.tick()
        }
        // 설정 변경 시 오버레이를 즉시 다시 반영(색·투명도·유휴·일시정지).
        status.onSettingsChanged = { [weak self] in self?.tick() }
        status.update(state: Decider.resolveState(inputSource.currentId), inputSourceId: inputSource.currentId)
    }

    func start() {
        let t = Timer(timeInterval: 0.15, repeats: true) { [weak self] _ in self?.tick() }
        RunLoop.main.add(t, forMode: .common)
        timer = t
    }

    private func tick() {
        // 일시정지면 아무것도 표시하지 않는다.
        if settings.paused {
            overlay.apply(.hidden)
            return
        }

        guard let app = NSWorkspace.shared.frontmostApplication else {
            hide("front=nil")
            return
        }

        // --- 매 틱 도는 값싼 구간(전부 로컬 호출 — 대상 앱에 묻지 않는다) ---
        let now = ProcessInfo.processInfo.systemUptime
        if app.processIdentifier != lastFrontPid {
            lastFrontPid = app.processIdentifier
            lastFocusChangeAt = now
            cachedReading = nil
        }
        // 포인터(마우스·트랙패드) 이동·드래그·스크롤도 입력 활동으로 볼지는 설정에 따른다.
        let sinceInput = InputActivity.secondsSinceLastInput(includePointerMove: settings.hideOnPointerMove)
        let lastInputAt = now - sinceInput
        let idleMs = Decider.millisSinceActivity(
            millisSinceLastInput: Self.millis(sinceInput),
            millisSinceFocusChange: Self.millis(now - lastFocusChangeAt))

        // 입력 중이면 어차피 숨김 → AX를 전혀 묻지 않는다. 이 게이트가 없으면 매 틱 대상 앱
        // (Chromium/Electron이면 특히 비싸다) 메인 스레드를 동기로 붙잡아 타이핑이 굼떠진다.
        guard Decider.isIdle(millisSinceInputActivity: idleMs, idleReappearMs: settings.idleReappearMs) else {
            checkedFocusLastTick = false
            hide("typing idle=\(idleMs)ms (AX 생략)")
            return
        }
        let checkedFocusPrevTick = checkedFocusLastTick
        checkedFocusLastTick = true

        // --- 유휴 구간: 포커스 요소만 확인(AX 1회), 바뀌었거나 새 입력이 있었으면 재판독 ---
        guard let focused = CaretReader.focusedElement(of: app) else {
            cachedReading = nil
            hide("focus=nil (포커스 없음 또는 AX 미신뢰)")
            return
        }
        let reading: CaretReading
        if let c = cachedReading, CFEqual(c.focusedElement, focused), cachedAt >= lastInputAt {
            reading = c
        } else {
            // 유휴가 이어지는 중에 입력 없이 포커스가 바뀌었으면(앱이 스스로 포커스 이동) 유휴를 다시 센다.
            // 타이핑 직후 첫 유휴 틱에서 발견한 변화는 그 입력(클릭·탭)이 이미 유휴를 리셋했으므로 제외 —
            // 여기서 또 리셋하면 표시가 한 번 더 늦어진다.
            let focusMoved = checkedFocusPrevTick && cachedReading.map { !CFEqual($0.focusedElement, focused) } == true
            reading = CaretReader.read(focused, of: app)
            cachedReading = reading
            cachedAt = now
            if focusMoved {
                lastFocusChangeAt = now
                checkedFocusLastTick = false // 유휴가 다시 차면 첫 틱으로 취급
                hide("focus moved without input → 유휴 재시작")
                return
            }
        }

        let anchor = reading.caret?.origin ?? reading.fieldFrame?.origin ?? reading.windowBounds.origin
        let snap = InputSnapshot(
            editableFocus: reading.editableFocus,
            caret: reading.caret,
            fieldFrame: reading.fieldFrame,
            activeWindowBounds: reading.windowBounds,
            screenBounds: Coord.screenBoundsTopLeft(containing: anchor),
            indicatorSize: overlay.indicatorSize,
            inputSourceId: inputSource.currentId,
            millisSinceInputActivity: idleMs)

        let view = Decider.decide(snap, idleReappearMs: settings.idleReappearMs)
        overlay.apply(view)
        log("front=\(app.localizedName ?? "?") role=\(reading.role) editable=\(reading.editableFocus) caret=\(reading.caret.map{"\($0)"} ?? "nil") idle=\(idleMs)ms → visible=\(view.visible)")
    }

    private func hide(_ reason: String) {
        overlay.apply(.hidden)
        log(reason)
    }

    private static func millis(_ seconds: Double) -> Int {
        Int(min(seconds * 1000, Double(Int.max / 2)))
    }

    // 1초에 한 번만 남기는 스로틀 로그(디버그 활성 시).
    private func log(_ s: String) {
        guard Log.enabled, Date().timeIntervalSince(lastLogAt) > 1 else { return }
        lastLogAt = Date()
        Log.line(s)
    }
}
