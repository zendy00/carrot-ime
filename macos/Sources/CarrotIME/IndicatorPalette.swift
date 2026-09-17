import AppKit

/// 인디케이터 색 팔레트 — Windows판과 동일 계열의 파스텔.
enum IndicatorPalette {
    struct Swatch { let name: String; let hex: String }

    static let backgrounds: [Swatch] = [
        .init(name: "블루", hex: "#3C8CFF"),
        .init(name: "퍼플", hex: "#A78BFA"),
        .init(name: "핑크", hex: "#F472B6"),
        .init(name: "코랄", hex: "#FB7185"),
        .init(name: "브라운", hex: "#B08968"),
        .init(name: "골드", hex: "#F5B841"),
        .init(name: "올리브", hex: "#A3B18A"),
        .init(name: "그린", hex: "#4ADE80"),
        .init(name: "틸", hex: "#2DD4BF"),
    ]
    static let foregrounds: [Swatch] = [
        .init(name: "흰색", hex: "#FFFFFF"),
        .init(name: "검정", hex: "#1A1A1A"),
    ]

    static func color(_ hex: String) -> NSColor {
        var s = hex
        if s.hasPrefix("#") { s.removeFirst() }
        guard s.count == 6, let v = UInt32(s, radix: 16) else { return .systemBlue }
        return NSColor(srgbRed: CGFloat((v >> 16) & 0xff) / 255,
                       green: CGFloat((v >> 8) & 0xff) / 255,
                       blue: CGFloat(v & 0xff) / 255,
                       alpha: 1)
    }
}
