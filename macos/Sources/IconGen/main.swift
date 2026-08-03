import AppKit

// 앱 아이콘(.iconset) 생성기 — 당근을 그려 표준 크기 PNG들을 낸다.
// build-app.sh가 이걸 돌린 뒤 iconutil로 .icns를 만든다. (에셋 바이너리 커밋 회피 + 재현성.)

func hex(_ s: String, _ a: CGFloat = 1) -> NSColor {
    var t = s; if t.hasPrefix("#") { t.removeFirst() }
    let v = UInt32(t, radix: 16) ?? 0
    return NSColor(srgbRed: CGFloat((v >> 16) & 0xff) / 255,
                   green: CGFloat((v >> 8) & 0xff) / 255,
                   blue: CGFloat(v & 0xff) / 255, alpha: a)
}

func drawCarrot(_ s: CGFloat) {
    // 배경: 따뜻한 라운드 사각형(squircle 느낌).
    let inset = s * 0.06
    let bg = NSBezierPath(roundedRect: NSRect(x: inset, y: inset, width: s - 2 * inset, height: s - 2 * inset),
                          xRadius: s * 0.225, yRadius: s * 0.225)
    NSGradient(colors: [hex("#FFF0DC"), hex("#FFD8A8")])?.draw(in: bg, angle: -90)

    let cx = s * 0.5
    let topY = s * 0.40      // 당근 몸통 상단(아래가 0인 Cocoa 좌표계 기준 위쪽)
    let tipY = s * 0.16      // 뾰족한 끝(아래)
    let halfW = s * 0.155

    // 몸통: 살짝 곡선진 삼각형.
    let body = NSBezierPath()
    body.move(to: NSPoint(x: cx - halfW, y: topY))
    body.curve(to: NSPoint(x: cx, y: tipY),
               controlPoint1: NSPoint(x: cx - halfW * 0.7, y: topY - (topY - tipY) * 0.5),
               controlPoint2: NSPoint(x: cx - halfW * 0.25, y: tipY + (topY - tipY) * 0.1))
    body.curve(to: NSPoint(x: cx + halfW, y: topY),
               controlPoint1: NSPoint(x: cx + halfW * 0.25, y: tipY + (topY - tipY) * 0.1),
               controlPoint2: NSPoint(x: cx + halfW * 0.7, y: topY - (topY - tipY) * 0.5))
    body.close()
    hex("#FF7A1A").setFill()
    body.fill()

    // 이랑(가로 홈) 몇 개.
    hex("#E8620A", 0.55).setStroke()
    for f in [0.25, 0.45, 0.65] {
        let y = topY - (topY - tipY) * CGFloat(f)
        let w = halfW * (1 - CGFloat(f)) * 1.4
        let line = NSBezierPath()
        line.lineWidth = s * 0.012
        line.move(to: NSPoint(x: cx - w, y: y))
        line.line(to: NSPoint(x: cx + w, y: y))
        line.stroke()
    }

    // 잎: 초록 잎 3장(몸통 위).
    hex("#3FB950").setFill()
    let leaf: [(CGFloat, CGFloat)] = [(-0.10, 0.16), (0.0, 0.20), (0.10, 0.16)]
    for (dx, h) in leaf {
        let base = NSPoint(x: cx + s * dx, y: topY - s * 0.01)
        let tip = NSPoint(x: cx + s * dx * 1.4, y: topY + s * h)
        let p = NSBezierPath()
        p.move(to: NSPoint(x: base.x - s * 0.05, y: base.y))
        p.curve(to: tip, controlPoint1: NSPoint(x: base.x - s * 0.02, y: base.y + s * h * 0.6),
                controlPoint2: NSPoint(x: tip.x - s * 0.03, y: tip.y))
        p.curve(to: NSPoint(x: base.x + s * 0.05, y: base.y),
                controlPoint1: NSPoint(x: tip.x + s * 0.03, y: tip.y),
                controlPoint2: NSPoint(x: base.x + s * 0.02, y: base.y + s * h * 0.6))
        p.close()
        p.fill()
    }
}

func makePNG(_ px: Int) -> Data {
    let rep = NSBitmapImageRep(
        bitmapDataPlanes: nil, pixelsWide: px, pixelsHigh: px,
        bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
        colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
    rep.size = NSSize(width: px, height: px)
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    drawCarrot(CGFloat(px))
    NSGraphicsContext.current?.flushGraphics()
    NSGraphicsContext.restoreGraphicsState()
    return rep.representation(using: .png, properties: [:])!
}

let out = CommandLine.arguments.count > 1 ? CommandLine.arguments[1] : "AppIcon.iconset"
try? FileManager.default.createDirectory(atPath: out, withIntermediateDirectories: true)

let items: [(String, Int)] = [
    ("icon_16x16", 16), ("icon_16x16@2x", 32),
    ("icon_32x32", 32), ("icon_32x32@2x", 64),
    ("icon_128x128", 128), ("icon_128x128@2x", 256),
    ("icon_256x256", 256), ("icon_256x256@2x", 512),
    ("icon_512x512", 512), ("icon_512x512@2x", 1024),
]
for (name, px) in items {
    try! makePNG(px).write(to: URL(fileURLWithPath: "\(out)/\(name).png"))
}
print("✓ \(out) (\(items.count) PNGs)")
