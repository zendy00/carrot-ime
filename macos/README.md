# CarrotIME — macOS

Windows판 CarrotIME를 macOS로 확장한 `macos` 브랜치의 Swift/AppKit 네이티브 앱.
캐럿 옆에 현재 입력기 상태(한 / A / あ / 中)를 띄우는 메뉴바 상주 앱이다. **Xcode 없이 SwiftPM만으로** 빌드된다.

## 상태

동작하는 앱이다. 메뉴바 상주 + 투명·클릭통과 오버레이 + 입력 소스 이벤트/캐럿 폴링이 배선돼 있고,
설정(색·투명도·유휴시간·일시정지·자동시작), 앱/메뉴바 아이콘, 브라우저 캐럿 폴백, Core 단위 테스트까지 있다.

## 구조 (함수형 코어 / 명령형 셸 — Windows판과 동일 원칙)

- `Sources/CarrotIMECore` — OS 접근 없는 순수 로직. 유일한 seam `Decider.decide(InputSnapshot) → IndicatorView`.
  Windows `CarrotIME.Core` 이식이되 상태 판별만 macOS 입력 소스 모델(ID 기반)로 교체.
- `Sources/CarrotIME` — AppKit 셸.
  - 어댑터: `InputSourceReader`(TIS + 전환 알림), `CaretReader`(AX 캐럿·프레임), `FocusInspector`(편집 판정), `AutoStart`(SMAppService).
  - UI: `OverlayWindow`(투명·클릭통과·전 Space), `StatusItemController`(메뉴바), `IndicatorPalette`.
  - 상태/조정: `IndicatorController`(오케스트레이터), `AppSettings`(UserDefaults), `Coord`(좌표변환), `Log`(진단).
- `Sources/IconGen` — 앱 아이콘(.iconset) 생성기(당근을 코드로 그림).
- `Tests/CarrotIMECoreTests` — Swift Testing 기반 Decider 테스트(`swift test`).
- `Sources/{ImeProbe,CaretProbe,BrowserCaretProbe}` — 조사용 프로브(참고).

## 상태 판별 모델 (Windows와의 차이)

macOS는 한/영 토글이 입력 소스 자체를 스왑한다(`com.apple.inputmethod.Korean.*` ↔ `com.apple.keylayout.ABC`).
따라서 **현재 입력 소스 ID 하나로** 상태가 갈리고, 전환은 `kTISNotifySelectedKeyboardInputSourceChanged`
**알림으로 이벤트 처리**된다(Windows의 조합모드 폴링 불필요). 캐럿 이동만 폴링해 유휴를 판정한다.

## 빌드 · 설치

개발용(빠른 반복):

```
cd macos
swift build
swift run CarrotIME
```

번들 `.app` 설치(자동시작·정식 실행):

```
cd macos
./build-app.sh                                   # release 빌드 + .app 조립 + 아이콘 + ad-hoc 서명
rm -rf /Applications/CarrotIME.app
cp -R build/CarrotIME.app /Applications/
open /Applications/CarrotIME.app
```

### 접근성 권한 (필수)
캐럿을 읽으려면 접근성(TCC) 권한이 필요하다. 실행 후 **시스템 설정 → 개인정보 보호 및 보안 → 손쉬운 사용**에서 CarrotIME를 켠다.

### 서명 — 재빌드해도 권한 유지
`build-app.sh`는 **로컬 자체 서명 인증서**(`CarrotIME Local`)가 있으면 그걸로 서명한다. 그러면
designated requirement가 인증서 리프에 고정돼 **재빌드/재설치해도 접근성 권한이 유지**된다.
최초 1회 인증서를 만든다(전용 키체인만 생성, 로그인 키체인/암호는 안 건드림):

```
cd macos
./tools/make-signing-cert.sh     # 1회
```

- 인증서가 없으면 `build-app.sh`는 **ad-hoc** 서명으로 폴백하는데, 이 경우 재빌드마다 서명(cdhash)이 바뀌어 권한이 무효화된다(`AXTrusted=false`). 그때는 `tccutil reset Accessibility com.carrotime.mac` 후 다시 켠다.
- 자체 서명으로 **처음 전환할 때**는 서명 신원이 바뀌므로 한 번은 `tccutil reset` + 재허용이 필요하다. 그 뒤로는 유지된다.
- 되돌리기: `security delete-keychain carrotime-signing.keychain-db`.

## 동작 · 설정

- 메뉴바에 **당근 위에 상태 글자를 겹친 아이콘**(가/A/あ). 텍스트 필드에 포커스하고 **설정한 유휴시간(기본 3초)** 가만히 두면 캐럿 옆에 인디케이터가 뜬다(타이핑 중엔 숨음 — 스펙대로).
- 메뉴: 일시정지 / 표시 지연 시간(1·2·3·5초) / 투명도 / 배경색 / 글자색 / 로그인 시 시작 / About / 종료. 설정은 UserDefaults 저장.
- **편집 판정**(`FocusInspector`): 편집 텍스트 역할(AXTextField/AXTextArea/AXComboBox/AXSearchField) + AXValue 설정 가능일 때만. 읽기 전용·본문은 제외.
- **브라우저 캐럿**: Safari(WebKit)는 정밀 캐럿. Chrome/Electron은 `AXManualAccessibility`로 AX를 켠 뒤, 캐럿 rect가 없으면(Chromium 한계) **필드 프레임 오른쪽**에 폴백. 그마저 없으면 창 우상단.
- **진단**: `CARROTIME_DEBUG=1` 이면 `/tmp/carrotime.log`에 AX 신뢰·포커스·표시 결정을 남긴다. (설치본은 `launchctl setenv CARROTIME_DEBUG 1` 후 재실행하면 세션 env 상속.)

## 테스트

```
cd macos
swift test        # Swift Testing (XCTest는 CLT 툴체인에 없어 미사용). Decider 판정·앵커 폴백 검증.
```

## 알려진 한계

- 로컬 자체 서명(`CarrotIME Local`)까지만. 배포용 Developer ID 서명/공증은 미구현(Gatekeeper 배포 불가, 로컬 실행용).
- **입력 소스 전환 HUD 억제 불가**: 전환 표시는 SIP 보호 시스템 에이전트(`TextInputSwitcher`)가 온디맨드로 그리며 공개 토글/억제 API가 없다(스파이크로 확인). 상주 인디케이터는 이와 무관하게 동작.
- Chromium은 AX 활성화 시 대상 앱 CPU/RAM이 다소 늘 수 있다. 멀티라인/캔버스 등은 창 폴백.

## 프로브 (조사용, 참고)

- `swift run ImeProbe` — 현재 입력 소스 + 전환 알림 관찰(권한 불필요).
- `swift run CaretProbe` — 최상단 앱 포커스 요소의 캐럿 rect 실측(접근성 권한 필요).
- `swift run BrowserCaretProbe` — 앱+시스템전역 포커스, role/caret/frame/sel/조상체인, `AXManualAccessibility` 세팅(브라우저 조사).
