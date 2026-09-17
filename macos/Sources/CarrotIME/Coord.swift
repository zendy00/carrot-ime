import AppKit

/// AX/Quartz는 top-left 원점 전역 좌표, NSWindow 배치는 bottom-left 원점 Cocoa 좌표.
/// 이 변환은 셸의 창 배치 경계에서만 일어난다 — Core는 전부 top-left로 다룬다.
enum Coord {
    /// 기준 화면(메뉴바가 있는 zero-origin 화면)의 Cocoa maxY.
    private static var primaryMaxY: CGFloat { NSScreen.screens.first?.frame.maxY ?? 0 }

    /// top-left 전역 점 → Cocoa(bottom-left) 점.
    static func topLeftToCocoa(_ p: CGPoint) -> NSPoint {
        NSPoint(x: p.x, y: primaryMaxY - p.y)
    }

    /// top-left 전역 점을 포함하는 화면의 경계를, top-left 전역 좌표 CGRect로.
    static func screenBoundsTopLeft(containing axPoint: CGPoint) -> CGRect {
        let cocoa = topLeftToCocoa(axPoint)
        let screen = NSScreen.screens.first(where: { NSMouseInRect(cocoa, $0.frame, false) })
            ?? NSScreen.main ?? NSScreen.screens[0]
        let f = screen.frame
        return CGRect(x: f.minX, y: primaryMaxY - f.maxY, width: f.width, height: f.height)
    }
}
