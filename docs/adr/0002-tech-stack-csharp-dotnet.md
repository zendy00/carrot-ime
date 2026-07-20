# 기술 스택으로 C# (.NET)을 선택한다

이 앱은 백그라운드 상주 + 전역 캐럿 추적(UI Automation·WinEvent 훅) + 투명·클릭통과·최상단 오버레이를 고빈도로 그리는 시스템 밀착형 도구다. **C# (.NET)** 으로 구현한다 — 관리형 UI Automation 클라이언트, 투명 오버레이 구현 용이, 빠른 개발 생산성 때문이며, self-contained / NativeAOT 배포로 .NET 런타임 의존을 없앨 수 있어 배포 부담도 낮다.

## Considered Options

- **C++ (Win32)** — 런타임 무의존·최소 풋프린트·GC 없는 오버레이로 이상적이지만, COM/UIA를 손수 다뤄야 하고 개발 비용이 가장 큼. 기각.
- **Rust (windows-rs)** — 무런타임·메모리 안전성 매력적이나 UIA·오버레이 예제·생태계가 얇고 러닝커브가 커 초기 속도가 느림. 기각.

## Consequences

기본은 관리형 코드지만 캐럿/IME/오버레이 API 상당수는 P/Invoke로 Win32를 직접 호출해야 한다. 배포는 self-contained 또는 NativeAOT 단일 exe를 지향해 사용자가 런타임을 따로 설치하지 않게 한다.
