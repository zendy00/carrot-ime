import AppKit
import ApplicationServices

// 메뉴바 상주 에이전트 진입점. Dock 아이콘 없이(.accessory) 뜬다.
final class AppDelegate: NSObject, NSApplicationDelegate {
    private var status: StatusItemController!
    private var controller: IndicatorController!

    func applicationDidFinishLaunching(_ notification: Notification) {
        // 접근성 권한 프롬프트(캐럿 읽기에 필수). 없으면 인디케이터는 숨겨지고 메뉴바만 동작.
        let opts = [kAXTrustedCheckOptionPrompt.takeUnretainedValue(): true] as CFDictionary
        _ = AXIsProcessTrustedWithOptions(opts)

        status = StatusItemController()
        controller = IndicatorController(status: status)
        controller.start()
    }
}

let app = NSApplication.shared
app.setActivationPolicy(.accessory)
let delegate = AppDelegate()
app.delegate = delegate
app.run()
