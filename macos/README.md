# CarrotIME — macOS (스켈레톤 단계)

Windows판 CarrotIME를 macOS로 확장하기 위한 `macos` 브랜치의 Swift/AppKit 작업 공간.
프로브 2개로 입력·캐럿 모델을 검증했고, 이제 실제 앱 스켈레톤이 있다.

## 구조 (함수형 코어 / 명령형 셸 — Windows판과 동일 원칙)

- `Sources/CarrotIMECore` — OS 접근 없는 순수 로직. 유일한 seam `Decider.decide(InputSnapshot) → IndicatorView`.
  Windows `CarrotIME.Core`의 이식이며, 상태 판별만 macOS 입력 소스 모델(ID 기반)로 교체됐다.
- `Sources/CarrotIME` — AppKit 셸. 어댑터(`InputSourceReader`=TIS 이벤트, `CaretReader`=AX),
  `OverlayWindow`(투명·클릭통과 오버레이), `StatusItemController`(메뉴바), `IndicatorController`(오케스트레이터).
- `Sources/{CaretProbe,ImeProbe}` — 초기 스파이크 프로브(검증 완료, 참고용).

## 앱 실행

```
cd macos
swift run CarrotIME
```

- 처음 실행 시 **접근성 권한(TCC)** 프롬프트. 손쉬운 사용에서 호스팅 터미널 앱을 허용 후 재실행.
- 메뉴바에 현재 상태 글자(가/A/あ)가 뜨고, TextEdit·메모 등 텍스트 필드에 포커스하면 캐럿 옆에 파란 인디케이터가 붙는다.
- **스켈레톤 한계**: 유휴 debounce는 데모용으로 꺼둠(항상 표시). 편집 판정은 휴리스틱(역할+settable),
  색상·투명도·유휴시간·자동시작 설정 미구현. 배포용 `.app` 번들·서명 미구현(현재 언번들 dev 실행).

## 초기 프로브 (참고)

## 요구

- macOS 13+ / Swift 6 (CLT 또는 Xcode). 이 저장소는 **Xcode 없이 SwiftPM만으로** 빌드된다.

## 빌드

```
cd macos
swift build
```

## 프로브

### 1) ImeProbe — 한/영·입력 소스 전환 관찰 (권한 불필요)

```
swift run ImeProbe
```

- 현재 입력 소스를 찍고, 전환 알림(`kTISNotifySelectedKeyboardInputSourceChanged`)을 구독한다.
- **확인할 것**: ABC ↔ 2-Set Korean 전환 시 `change` 줄이 뜨는가. 그리고 2-Set Korean 안에서
  한/영 토글(Caps Lock 등) 시에도 신호가 오는가 — 아니면 한글 소스 하나로만 보이는가(미검증 항목).
- Ctrl+C 로 종료.

### 2) CaretProbe — AX 캐럿 rect 실측 (접근성 권한 필요)

```
swift run CaretProbe
```

- **접근성 권한(TCC)** 이 필요하다. 처음 실행하면 권한 요청이 뜬다.
  시스템 설정 → 개인정보 보호 및 보안 → **손쉬운 사용**에서 이 프로세스를 호스팅하는
  **터미널 앱**을 허용한 뒤 다시 실행한다.
- 30초간 0.5s마다 최상단 앱의 포커스 요소 캐럿 rect를 찍는다. 실행 후 TextEdit·메모·Chrome·터미널
  등을 클릭해 타이핑하며 rect가 따라오는지 본다.
- **확인할 것**: 어떤 앱이 rect를 주고 어떤 앱이 `<none>`인가(폴백 정책 근거), 빈 입력란/선택 영역의
  rect 폭(Decider `MaxCaretWidth` 대응).

## 다음

- 두 프로브 결과 → macOS용 `InputSnapshot`/상태 판별 모델 확정.
- 별도 스파이크: 입력 소스 전환 시 뜨는 macOS 내장 HUD 억제 실현 가능성(공식 API 없음 — 최대 리스크).
