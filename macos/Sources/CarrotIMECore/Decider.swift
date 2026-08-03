import CoreGraphics

/// 유일한 seam. 모든 결정 로직(상태 판별·라벨·위치·표시 여부)이 이 순수 함수 뒤에 모인다.
/// OS 접근은 전혀 없다 — InputSnapshot 값만 보고 IndicatorView 값을 낸다.
/// (Windows CarrotIME.Core.Decider의 이식. 상태 판별만 macOS 입력 소스 모델로 교체됐다.)
public enum Decider {
    /// 캐럿과 인디케이터 사이 간격 — 방금 친 글자를 가리지 않도록.
    static let caretGapX: CGFloat = 4
    /// 캐럿(세로 막대)으로 볼 수 있는 최대 폭. 이보다 넓으면 실제 캐럿이 아니라 입력 박스·선택 영역.
    static let maxCaretWidth: CGFloat = 20
    /// 고정 폴백 위치(활성 창 우상단)의 안쪽 여백.
    static let fallbackMargin: CGFloat = 6

    /// 입력 중에는 숨기고, 이만큼 유휴하면 다시 표시(ms). 사용자가 설정으로 바꿀 수 있다.
    public static let defaultIdleReappearMs = 20_000

    /// 현재 입력 소스 ID로 입력 상태를 판별한다.
    /// macOS는 한/영 토글이 입력 소스 자체를 스왑하므로(Korean.* ↔ keylayout.ABC) ID 하나로 갈린다.
    public static func resolveState(_ inputSourceId: String) -> InputState {
        let id = inputSourceId
        if id.contains("inputmethod.Korean") { return .hangul }
        if id.contains("Japanese") { return .otherIme }
        if id.contains(".SCIM") || id.contains(".TCIM") || id.lowercased().contains("chinese") { return .otherIme }
        // com.apple.keylayout.* 및 그 외 라틴 레이아웃 → 영문.
        return .english
    }

    /// 오버레이(캐럿 옆) 라벨. 한글=한, 영문=A, 기타 IME는 언어별 글자.
    public static func label(_ state: InputState, inputSourceId: String = "") -> String {
        switch state {
        case .hangul: return "한"
        case .english: return "A"
        case .otherIme: return otherImeLabel(inputSourceId)
        }
    }

    /// 트레이(메뉴바) 라벨. 오버레이와 달리 한글=가.
    public static func trayLabel(_ state: InputState, inputSourceId: String = "") -> String {
        switch state {
        case .hangul: return "가"
        case .english: return "A"
        case .otherIme: return otherImeLabel(inputSourceId)
        }
    }

    private static func otherImeLabel(_ id: String) -> String {
        if id.contains("Japanese") { return "あ" }
        if id.contains(".SCIM") || id.contains(".TCIM") || id.lowercased().contains("chinese") { return "中" }
        return "IME"
    }

    /// 스냅샷 하나를 받아 인디케이터를 어떻게 그릴지 결정한다.
    public static func decide(_ s: InputSnapshot, idleReappearMs: Int = defaultIdleReappearMs) -> IndicatorView {
        let state = resolveState(s.inputSourceId)
        let text = label(state, inputSourceId: s.inputSourceId)

        // 편집 가능한 텍스트 포커스가 있을 때만 표시.
        guard s.editableFocus else { return .hidden }

        // 입력 중(최근 캐럿 이동)에는 숨기고, 설정된 유휴 시간이 지나면 다시 표시.
        if s.millisSinceInputActivity < idleReappearMs { return .hidden }

        // 캐럿을 얻으면 오른쪽에(방금 친 글자를 안 가리게), 넓은 rect(입력 박스·선택 영역)면
        // 왼쪽 바깥에, 캐럿을 못 얻으면 활성 창 우상단에 폴백.
        let desired: CGPoint
        if let caret = s.caret {
            if caret.width <= maxCaretWidth {
                desired = CGPoint(x: caret.maxX + caretGapX, y: caret.minY)
            } else {
                desired = CGPoint(x: caret.minX - caretGapX - s.indicatorSize.width, y: caret.minY)
            }
        } else {
            desired = fallbackTopRight(s.activeWindowBounds, s.indicatorSize)
        }

        return IndicatorView(visible: true, label: text,
                             position: clampToScreen(desired, s.indicatorSize, s.screenBounds))
    }

    // 활성 창 우상단 안쪽.
    static func fallbackTopRight(_ window: CGRect, _ size: CGSize) -> CGPoint {
        CGPoint(x: window.maxX - size.width - fallbackMargin, y: window.minY + fallbackMargin)
    }

    // 인디케이터가 화면 밖으로 잘리지 않도록 대상 화면 경계 안으로 보정.
    static func clampToScreen(_ desired: CGPoint, _ size: CGSize, _ screen: CGRect) -> CGPoint {
        let maxX = max(screen.minX, screen.maxX - size.width)
        let maxY = max(screen.minY, screen.maxY - size.height)
        let x = min(max(desired.x, screen.minX), maxX)
        let y = min(max(desired.y, screen.minY), maxY)
        return CGPoint(x: x, y: y)
    }
}
