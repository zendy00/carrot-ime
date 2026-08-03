# CarrotIME — macOS (스파이크 단계)

Windows판 CarrotIME를 macOS로 확장하기 위한 `macos` 브랜치의 Swift/AppKit 작업 공간.
지금은 **실현 가능성 검증용 프로브 2개**만 있다. 두 프로브 결과로 실제 앱의 입력·상태 모델을 확정한 뒤 앱 타겟을 붙인다.

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
