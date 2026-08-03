import AppKit
import CarrotIMECore

/// 캐럿 옆 라벨을 그리는 커스텀 뷰 — 둥근 배경에 글자. 색은 설정에서 온다.
final class IndicatorLabelView: NSView {
    var label: String = "A" { didSet { needsDisplay = true } }
    var bgColor: NSColor = .systemBlue { didSet { needsDisplay = true } }
    var fgColor: NSColor = .white { didSet { needsDisplay = true } }

    override func draw(_ dirtyRect: NSRect) {
        let r = bounds.insetBy(dx: 1, dy: 1)
        bgColor.setFill()
        NSBezierPath(roundedRect: r, xRadius: r.height / 2, yRadius: r.height / 2).fill()

        let ps = NSMutableParagraphStyle()
        ps.alignment = .center
        let attrs: [NSAttributedString.Key: Any] = [
            .font: NSFont.systemFont(ofSize: 13, weight: .semibold),
            .foregroundColor: fgColor,
            .paragraphStyle: ps,
        ]
        let s = label as NSString
        let size = s.size(withAttributes: attrs)
        s.draw(in: NSRect(x: 0, y: (bounds.height - size.height) / 2, width: bounds.width, height: size.height),
               withAttributes: attrs)
    }
}

/// 투명·클릭통과·최상단·전(全) Space 오버레이 창. IndicatorView가 지시한 위치에 라벨을 그린다.
final class OverlayWindow {
    let indicatorSize = CGSize(width: 26, height: 22)
    private let window: NSWindow
    private let view: IndicatorLabelView

    init() {
        let frame = NSRect(origin: .zero, size: indicatorSize)
        window = NSWindow(contentRect: frame, styleMask: .borderless, backing: .buffered, defer: false)
        window.isOpaque = false
        window.backgroundColor = .clear
        window.hasShadow = false
        window.level = .floating
        window.ignoresMouseEvents = true            // 클릭 통과
        window.collectionBehavior = [.canJoinAllSpaces, .stationary, .ignoresCycle]
        view = IndicatorLabelView(frame: frame)
        window.contentView = view
    }

    func apply(_ v: CarrotIMECore.IndicatorView) {
        guard v.visible else {
            window.orderOut(nil)
            return
        }
        let s = AppSettings.shared
        view.label = v.label
        view.bgColor = IndicatorPalette.color(s.backgroundHex)
        view.fgColor = IndicatorPalette.color(s.foregroundHex)
        window.alphaValue = CGFloat(s.opacity)
        window.setFrameTopLeftPoint(Coord.topLeftToCocoa(v.position))
        window.orderFrontRegardless()
    }
}
