import CoreGraphics

/// 마지막 키/마우스 입력 이후 경과 초. CGEventSource 로컬 질의뿐이다(권한 불필요).
///
/// 예전에는 "캐럿이 움직였나"로 입력을 감지했는데, 그러려면 매 틱 AX로 캐럿을 읽어야 했고
/// AX 질의는 대상 앱 메인 스레드를 동기로 붙잡는다 — 타이핑하는 바로 그 스레드라 입력이 굼떠진다.
/// 부수적으로, 캐럿을 못 얻는 앱(Chromium 폴백)에서 입력 활동을 감지 못 하던 한계도 사라진다.
enum InputActivity {
    // 키 입력과 클릭(캐럿을 옮긴다). 수정키 단독 입력(flagsChanged)은 제외한다 — Caps Lock·오른쪽 ⌘ 등
    // 한/영 전환키가 여기 속하는데, 전환 직후야말로 바뀐 상태를 보여줘야 할 때라 숨기면 안 된다
    // (예전 캐럿 이동 기준에서도 전환키는 활동이 아니었다).
    private static let keyboardAndClicks: [CGEventType] = [
        .keyDown, .leftMouseDown, .rightMouseDown, .otherMouseDown,
    ]
    private static let pointerMoves: [CGEventType] = [
        .mouseMoved, .leftMouseDragged, .rightMouseDragged, .otherMouseDragged, .scrollWheel,
    ]

    static func secondsSinceLastInput(includePointerMove: Bool) -> Double {
        let types = includePointerMove ? keyboardAndClicks + pointerMoves : keyboardAndClicks
        return types.map(since).min() ?? .greatestFiniteMagnitude
    }

    private static func since(_ type: CGEventType) -> Double {
        CGEventSource.secondsSinceLastEventType(.combinedSessionState, eventType: type)
    }
}
