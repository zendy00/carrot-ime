import AppKit
import CarrotIMECore

/// 메뉴바 상주 아이템 — 현재 상태 글자(가/A/あ)를 표시하고 종료 메뉴를 준다.
/// (Windows TrayIcon 대응. 인디케이터가 숨겨져 있어도 상태를 알 수 있게.)
final class StatusItemController {
    private let item: NSStatusItem

    init() {
        item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        item.button?.title = "🥕"

        let menu = NSMenu()
        menu.addItem(withTitle: "CarrotIME (macOS 스켈레톤)", action: nil, keyEquivalent: "")
        menu.addItem(.separator())
        menu.addItem(withTitle: "종료", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        item.menu = menu
    }

    func update(state: InputState, inputSourceId: String) {
        item.button?.title = Decider.trayLabel(state, inputSourceId: inputSourceId)
    }
}
