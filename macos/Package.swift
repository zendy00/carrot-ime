// swift-tools-version:5.9
import PackageDescription

// CarrotIME macOS.
// - CarrotIMECore: OS 접근 없는 순수 로직(Windows CarrotIME.Core의 Swift 이식). 유일한 seam은 Decider.decide.
// - CarrotIME: AppKit 명령형 셸(메뉴바 + 투명 오버레이 + 이벤트/타이머).
// - CaretProbe/ImeProbe: 초기 스파이크 프로브(검증 완료, 참고용 보존).
let package = Package(
    name: "CarrotIMEMac",
    platforms: [.macOS(.v13)],
    targets: [
        .target(name: "CarrotIMECore", path: "Sources/CarrotIMECore"),
        .testTarget(name: "CarrotIMECoreTests", dependencies: ["CarrotIMECore"], path: "Tests/CarrotIMECoreTests"),
        .executableTarget(
            name: "CarrotIME",
            dependencies: ["CarrotIMECore"],
            path: "Sources/CarrotIME"),
        .executableTarget(name: "CaretProbe", path: "Sources/CaretProbe"),
        .executableTarget(name: "ImeProbe", path: "Sources/ImeProbe"),
        .executableTarget(name: "BrowserCaretProbe", path: "Sources/BrowserCaretProbe"),
    ]
)
