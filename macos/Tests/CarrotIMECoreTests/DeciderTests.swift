import CoreGraphics
import Testing

@testable import CarrotIMECore

private let indicator = CGSize(width: 26, height: 22)
private let screen = CGRect(x: 0, y: 0, width: 3000, height: 2000)
private let window = CGRect(x: 100, y: 100, width: 800, height: 600)

private func snap(caret: CGRect?, frame: CGRect?, editable: Bool = true, idleElapsed: Int = 999_999)
    -> InputSnapshot
{
    InputSnapshot(
        editableFocus: editable,
        caret: caret,
        fieldFrame: frame,
        activeWindowBounds: window,
        screenBounds: screen,
        indicatorSize: indicator,
        inputSourceId: "com.apple.keylayout.ABC",
        millisSinceInputActivity: idleElapsed)
}

private func near(_ a: CGFloat, _ b: CGFloat) -> Bool { abs(a - b) < 0.5 }

// MARK: 상태·라벨

@Test func resolveStateFromInputSourceId() {
    #expect(Decider.resolveState("com.apple.inputmethod.Korean.2SetKorean") == .hangul)
    #expect(Decider.resolveState("com.apple.keylayout.ABC") == .english)
    #expect(Decider.resolveState("com.apple.inputmethod.Japanese") == .otherIme)
    // 구름입력기: 한글 레이아웃은 한글, 라틴 레이아웃은 영문. 한/영 토글은 ABC로 스왑됨.
    #expect(Decider.resolveState("org.youknowone.inputmethod.Gureum.han2") == .hangul)
    #expect(Decider.resolveState("org.youknowone.inputmethod.Gureum.han3final") == .hangul)
    #expect(Decider.resolveState("org.youknowone.inputmethod.Gureum.qwerty") == .english)
    #expect(Decider.resolveState("org.youknowone.inputmethod.Gureum.roman") == .english)
}

@Test func labels() {
    #expect(Decider.label(.hangul) == "한")
    #expect(Decider.trayLabel(.hangul) == "가")
    #expect(Decider.label(.english) == "A")
}

// MARK: 표시/숨김

@Test func hiddenWhenNoEditableFocus() {
    let v = Decider.decide(snap(caret: CGRect(x: 500, y: 500, width: 2, height: 16), frame: nil, editable: false),
                           idleReappearMs: 0)
    #expect(v.visible == false)
}

@Test func hiddenWhileTypingWithinIdleWindow() {
    let v = Decider.decide(snap(caret: CGRect(x: 1, y: 1, width: 2, height: 16), frame: nil, idleElapsed: 500),
                           idleReappearMs: 3000)
    #expect(v.visible == false)
}

// MARK: 앵커 3단 폴백

// 1) 정밀 캐럿(얇음) → 캐럿 오른쪽 + 간격.
@Test func preciseCaretPlacesToRight() {
    let caret = CGRect(x: 592, y: 800, width: 3, height: 24)
    let v = Decider.decide(snap(caret: caret, frame: CGRect(x: 464, y: 799, width: 601, height: 26)),
                           idleReappearMs: 0)
    #expect(v.visible)
    #expect(near(v.position.x, caret.maxX + 4))
    #expect(near(v.position.y, caret.minY))
}

// 2) Chromium식 퇴화 캐럿(0x0) → 캐럿 무시하고 필드 프레임 오른쪽 가장자리에 세로 중앙 정렬.
@Test func degenerateCaretFallsBackToFieldFrame() {
    let frame = CGRect(x: 464, y: 800, width: 601, height: 26)
    let v = Decider.decide(snap(caret: CGRect(x: -196, y: -934, width: 0, height: 0), frame: frame),
                           idleReappearMs: 0)
    #expect(v.visible)
    #expect(near(v.position.x, frame.maxX + 4))
    #expect(near(v.position.y, frame.midY - indicator.height / 2))
}

// 3) 캐럿도 프레임도 못 쓰면 → 활성 창 우상단 폴백.
@Test func noUsableAnchorFallsBackToWindowCorner() {
    let v = Decider.decide(snap(caret: nil, frame: nil), idleReappearMs: 0)
    #expect(v.visible)
    #expect(near(v.position.x, window.maxX - indicator.width - 6))
    #expect(near(v.position.y, window.minY + 6))
}

// 거대한 웹영역 프레임(문서 전체)은 필드로 안 봄 → 창 폴백.
@Test func hugeWebAreaFrameIsRejected() {
    let huge = CGRect(x: 110, y: -880, width: 1309, height: 5132)
    let v = Decider.decide(snap(caret: nil, frame: huge), idleReappearMs: 0)
    #expect(near(v.position.x, window.maxX - indicator.width - 6))
}

// 화면 밖으로 나가면 화면 안으로 보정.
@Test func clampToScreen() {
    let caret = CGRect(x: 2999, y: 1999, width: 2, height: 16)
    let v = Decider.decide(snap(caret: caret, frame: nil), idleReappearMs: 0)
    #expect(v.position.x + indicator.width <= screen.maxX)
    #expect(v.position.y + indicator.height <= screen.maxY)
}

// MARK: 표시 게이트 — AX를 묻기 전에 표시 여부를 먼저 판정
// AX 질의는 대상 앱 메인 스레드를 동기로 붙잡는 비싼 호출이라, 셸이 "어차피 안 보일 상황"을
// 미리 걸러 건너뛸 수 있어야 한다.

@Test func shouldShowIsFalseWithoutEditableFocus() {
    #expect(!Decider.shouldShow(editableFocus: false, millisSinceInputActivity: 60_000, idleReappearMs: 3_000))
}

@Test func shouldShowIsFalseWhileInputIsRecent() {
    #expect(!Decider.isIdle(millisSinceInputActivity: 1_000, idleReappearMs: 3_000))
    #expect(!Decider.shouldShow(editableFocus: true, millisSinceInputActivity: 1_000, idleReappearMs: 3_000))
}

@Test func shouldShowIsTrueOnceIdleThresholdIsReached() {
    #expect(Decider.isIdle(millisSinceInputActivity: 3_000, idleReappearMs: 3_000))
    #expect(Decider.shouldShow(editableFocus: true, millisSinceInputActivity: 3_000, idleReappearMs: 3_000))
}

@Test func shouldShowWithZeroThresholdIsTrueEvenWhileTyping() {
    #expect(Decider.shouldShow(editableFocus: true, millisSinceInputActivity: 0, idleReappearMs: 0))
}

// decide의 표시 여부는 shouldShow와 반드시 일치해야 한다 — 셸이 게이트로 AX를 건너뛰는 이상,
// 둘이 어긋나면 인디케이터가 위치 없이 뜨거나 안 뜬다.
@Test(arguments: [(true, 0), (true, 1_000), (true, 3_000), (true, 60_000), (false, 60_000)])
func decideVisibilityMatchesShouldShow(editable: Bool, elapsed: Int) {
    let caret = CGRect(x: 100, y: 200, width: 2, height: 16)
    let v = Decider.decide(snap(caret: caret, frame: nil, editable: editable, idleElapsed: elapsed), idleReappearMs: 3_000)
    #expect(v.visible == Decider.shouldShow(editableFocus: editable, millisSinceInputActivity: elapsed, idleReappearMs: 3_000))
}

// MARK: 입력 활동 합성 — 마지막 키/마우스 입력과 포커스 변화 중 더 최근 것이 기준

@Test func activityTakesTheMoreRecentOfInputAndFocusChange() {
    // 포커스가 바뀐 지 2초, 마지막 입력은 30초 전 → 2초(포커스 변화가 더 최근).
    #expect(Decider.millisSinceActivity(millisSinceLastInput: 30_000, millisSinceFocusChange: 2_000) == 2_000)
    // 입력이 더 최근이면 입력 기준.
    #expect(Decider.millisSinceActivity(millisSinceLastInput: 500, millisSinceFocusChange: 60_000) == 500)
}
