import CoreGraphics

/// decide가 필요로 하는 모든 것을 담은 순수 값 스냅샷. OS 어댑터가 만들어 넘긴다.
/// 좌표는 전부 **top-left 원점 전역(AX/Quartz) 좌표**로 통일한다 — Cocoa 변환은 셸의 창 배치에서만.
public struct InputSnapshot {
    public let editableFocus: Bool
    public let caret: CGRect?
    public let activeWindowBounds: CGRect
    public let screenBounds: CGRect
    public let indicatorSize: CGSize
    /// 현재 선택된 키보드 입력 소스 ID (예: com.apple.inputmethod.Korean.2SetKorean).
    /// Windows판의 KeyboardLangId+ConversionMode를 대체한다 — macOS는 소스 스왑이 곧 한/영.
    public let inputSourceId: String
    /// 마지막 입력 활동(캐럿 이동) 이후 경과 밀리초. 알 수 없으면 큰 값(유휴로 간주).
    public let millisSinceInputActivity: Int

    public init(
        editableFocus: Bool,
        caret: CGRect?,
        activeWindowBounds: CGRect,
        screenBounds: CGRect,
        indicatorSize: CGSize,
        inputSourceId: String,
        millisSinceInputActivity: Int
    ) {
        self.editableFocus = editableFocus
        self.caret = caret
        self.activeWindowBounds = activeWindowBounds
        self.screenBounds = screenBounds
        self.indicatorSize = indicatorSize
        self.inputSourceId = inputSourceId
        self.millisSinceInputActivity = millisSinceInputActivity
    }
}
