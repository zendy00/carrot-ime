// swift-tools-version:5.9
import PackageDescription

// CarrotIME macOS — 초기 스파이크 단계.
// 지금은 실현 가능성 검증용 프로브 두 개만 있고, 검증이 끝나면 여기에 실제 앱 타겟이 붙는다.
let package = Package(
    name: "CarrotIMEMac",
    platforms: [.macOS(.v13)],
    targets: [
        // AX 캐럿 rect 프로브 — 접근성 권한(TCC) 필요.
        .executableTarget(name: "CaretProbe", path: "Sources/CaretProbe"),
        // 한/영·입력 소스 전환 알림 프로브 — 권한 불필요.
        .executableTarget(name: "ImeProbe", path: "Sources/ImeProbe"),
    ]
)
