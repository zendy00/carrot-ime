import CoreGraphics

/// decide의 출력 값 객체 — 인디케이터를 어떻게 그릴지. position은 top-left 전역 좌표.
public struct IndicatorView: Equatable {
    public let visible: Bool
    public let label: String
    public let position: CGPoint

    public init(visible: Bool, label: String, position: CGPoint) {
        self.visible = visible
        self.label = label
        self.position = position
    }

    public static let hidden = IndicatorView(visible: false, label: "", position: .zero)
}
