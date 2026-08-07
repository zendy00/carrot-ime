import AppKit
import CarrotIMECore

/// 메뉴바 상주 아이템 — 현재 상태 글자(가/A/あ)를 표시하고 설정 메뉴를 준다.
/// (Windows TrayIcon 대응.) 설정 변경은 즉시 저장되고 onSettingsChanged로 셸에 통지된다.
final class StatusItemController: NSObject {
    private let item: NSStatusItem
    private let settings = AppSettings.shared

    /// 설정이 바뀌면 호출 — 오케스트레이터가 오버레이를 즉시 다시 반영하도록.
    var onSettingsChanged: (() -> Void)?

    private enum Tag { static let pause = 1; static let autostart = 2; static let pointerHide = 3 }

    private let idlePresets: [(String, Int)] = [
        ("1초", 1_000), ("2초", 2_000), ("3초", 3_000), ("5초", 5_000),
    ]
    private let opacityPresets: [(String, Double)] = [
        ("20%", 0.2), ("40%", 0.4), ("60%", 0.6), ("75%", 0.75), ("100%", 1.0),
    ]

    override init() {
        item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        super.init()
        item.button?.imagePosition = .imageOnly
        item.button?.image = trayImage("A") // 초기 플레이스홀더(곧 update가 교체).
        item.menu = buildMenu()
        syncStates()
    }

    func update(state: InputState, inputSourceId: String) {
        guard let button = item.button else { return }
        let letter = Decider.trayLabel(state, inputSourceId: inputSourceId)
        button.imagePosition = .imageOnly
        button.title = ""
        button.image = trayImage(letter)
    }

    // 당근 위에 상태 글자를 겹쳐 그린 단일 메뉴바 이미지.
    private func trayImage(_ letter: String) -> NSImage {
        let size = NSSize(width: 22, height: 20)
        let image = NSImage(size: size)
        image.lockFocus()

        // 당근(주황) 배경.
        if let carrot = NSImage(systemSymbolName: "carrot", accessibilityDescription: "CarrotIME") {
            let cfg = NSImage.SymbolConfiguration(pointSize: 19, weight: .regular)
                .applying(NSImage.SymbolConfiguration(paletteColors: [.systemOrange]))
            let c = carrot.withSymbolConfiguration(cfg) ?? carrot
            let side: CGFloat = 19
            c.draw(in: NSRect(x: (size.width - side) / 2, y: (size.height - side) / 2, width: side, height: side),
                   from: .zero, operation: .sourceOver, fraction: 1)
        }

        // 글자를 가운데 겹쳐(흰색 굵게 + 대비용 그림자).
        let ps = NSMutableParagraphStyle()
        ps.alignment = .center
        let shadow = NSShadow()
        shadow.shadowColor = NSColor.black.withAlphaComponent(0.85)
        shadow.shadowBlurRadius = 1.5
        shadow.shadowOffset = .zero
        let attrs: [NSAttributedString.Key: Any] = [
            .font: NSFont.systemFont(ofSize: 11, weight: .heavy),
            .foregroundColor: NSColor.white,
            .paragraphStyle: ps,
            .shadow: shadow,
        ]
        let text = letter as NSString
        let ts = text.size(withAttributes: attrs)
        text.draw(in: NSRect(x: 0, y: (size.height - ts.height) / 2 - 0.5, width: size.width, height: ts.height),
                  withAttributes: attrs)

        image.unlockFocus()
        image.isTemplate = false // 컬러 아이콘.
        return image
    }

    // MARK: - 메뉴 구성

    private func buildMenu() -> NSMenu {
        let menu = NSMenu()
        menu.addItem(withTitle: "CarrotIME", action: nil, keyEquivalent: "")
        menu.addItem(.separator())

        let pause = NSMenuItem(title: "일시정지", action: #selector(togglePause), keyEquivalent: "")
        pause.target = self
        pause.tag = Tag.pause
        menu.addItem(pause)

        let pointerHide = NSMenuItem(title: "마우스 움직이면 숨김", action: #selector(togglePointerHide), keyEquivalent: "")
        pointerHide.target = self
        pointerHide.tag = Tag.pointerHide
        menu.addItem(pointerHide)

        menu.addItem(submenu("표시 지연 시간", idlePresets.map {
            mkItem($0.0, #selector(pickIdle(_:)), NSNumber(value: $0.1))
        }))
        menu.addItem(submenu("투명도", opacityPresets.map {
            mkItem($0.0, #selector(pickOpacity(_:)), NSNumber(value: $0.1))
        }))
        menu.addItem(submenu("배경색", IndicatorPalette.backgrounds.map {
            colorItem($0.name, #selector(pickBg(_:)), $0.hex)
        }))
        menu.addItem(submenu("글자색", IndicatorPalette.foregrounds.map {
            colorItem($0.name, #selector(pickFg(_:)), $0.hex)
        }))

        menu.addItem(.separator())
        let auto = NSMenuItem(title: "로그인 시 시작", action: #selector(toggleAutoStart), keyEquivalent: "")
        auto.target = self
        auto.tag = Tag.autostart
        menu.addItem(auto)

        menu.addItem(.separator())
        let about = menu.addItem(withTitle: "About CarrotIME", action: #selector(about), keyEquivalent: "")
        about.target = self
        menu.addItem(withTitle: "종료", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        return menu
    }

    private func mkItem(_ title: String, _ action: Selector, _ rep: Any) -> NSMenuItem {
        let it = NSMenuItem(title: title, action: action, keyEquivalent: "")
        it.target = self
        it.representedObject = rep
        return it
    }

    // 이름 앞에 색상 견본을 붙인 메뉴 항목.
    private func colorItem(_ name: String, _ action: Selector, _ hex: String) -> NSMenuItem {
        let it = mkItem(name, action, hex as NSString)
        it.image = swatch(hex)
        return it
    }

    private func swatch(_ hex: String) -> NSImage {
        let size = NSSize(width: 14, height: 14)
        let image = NSImage(size: size)
        image.lockFocus()
        let rect = NSRect(x: 1, y: 1, width: 12, height: 12)
        let path = NSBezierPath(roundedRect: rect, xRadius: 3, yRadius: 3)
        IndicatorPalette.color(hex).setFill()
        path.fill()
        NSColor.separatorColor.setStroke()
        path.lineWidth = 0.5
        path.stroke()
        image.unlockFocus()
        return image
    }

    private func submenu(_ title: String, _ items: [NSMenuItem]) -> NSMenuItem {
        let parent = NSMenuItem(title: title, action: nil, keyEquivalent: "")
        let sub = NSMenu()
        items.forEach(sub.addItem)
        parent.submenu = sub
        return parent
    }

    // MARK: - 액션

    @objc private func togglePause() { settings.paused.toggle(); changed() }
    @objc private func togglePointerHide() { settings.hideOnPointerMove.toggle(); changed() }
    @objc private func pickIdle(_ s: NSMenuItem) {
        settings.idleReappearMs = (s.representedObject as? NSNumber)?.intValue ?? settings.idleReappearMs
        changed()
    }
    @objc private func pickOpacity(_ s: NSMenuItem) {
        settings.opacity = (s.representedObject as? NSNumber)?.doubleValue ?? settings.opacity
        changed()
    }
    @objc private func pickBg(_ s: NSMenuItem) {
        settings.backgroundHex = (s.representedObject as? String) ?? settings.backgroundHex
        changed()
    }
    @objc private func pickFg(_ s: NSMenuItem) {
        settings.foregroundHex = (s.representedObject as? String) ?? settings.foregroundHex
        changed()
    }
    @objc private func toggleAutoStart() {
        let target = !AutoStart.isEnabled
        if case .failure(let err) = AutoStart.setEnabled(target) {
            let a = NSAlert()
            a.messageText = "로그인 시 시작을 변경하지 못했습니다"
            a.informativeText = "번들된 CarrotIME.app(가급적 /Applications)으로 실행해야 로그인 항목을 등록할 수 있습니다.\n(\(err.localizedDescription))"
            a.addButton(withTitle: "확인")
            a.runModal()
        }
        syncStates()
    }

    @objc private func about() {
        let a = NSAlert()
        a.messageText = "CarrotIME (macOS)"
        a.informativeText = "캐럿 옆 입력 상태 표시기.\nmade by zendy"
        a.addButton(withTitle: "확인")
        a.runModal()
    }

    private func changed() {
        syncStates()
        onSettingsChanged?()
    }

    // 현재 설정에 맞춰 체크마크 갱신.
    private func syncStates() {
        guard let menu = item.menu else { return }
        for it in menu.items {
            if it.tag == Tag.pause { it.state = settings.paused ? .on : .off }
            if it.tag == Tag.pointerHide { it.state = settings.hideOnPointerMove ? .on : .off }
            if it.tag == Tag.autostart { it.state = AutoStart.isEnabled ? .on : .off }
            guard let sub = it.submenu else { continue }
            for sit in sub.items { sit.state = isSelected(sit) ? .on : .off }
        }
    }

    private func isSelected(_ it: NSMenuItem) -> Bool {
        switch it.action {
        case #selector(pickIdle(_:)): return (it.representedObject as? NSNumber)?.intValue == settings.idleReappearMs
        case #selector(pickOpacity(_:)): return (it.representedObject as? NSNumber)?.doubleValue == settings.opacity
        case #selector(pickBg(_:)): return (it.representedObject as? String) == settings.backgroundHex
        case #selector(pickFg(_:)): return (it.representedObject as? String) == settings.foregroundHex
        default: return false
        }
    }
}
