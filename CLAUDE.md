# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트

CarrotIME — 텍스트 캐럿 옆에 현재 입력기 상태(한 / A / あ / 中)를 띄우는 Windows 트레이 앱.
net9.0-windows, WinForms, x64 전용, 관리자 권한(UAC)으로 실행된다(ADR-0003).

## 명령

```
dotnet test tests/CarrotIME.Core.Tests                          # 전체 테스트
dotnet test tests/CarrotIME.Core.Tests --filter "Wide_rect"     # 단일 테스트(이름 부분 일치)
dotnet build src/CarrotIME                                      # 컴파일 확인
dotnet publish src/CarrotIME -p:PublishProfile=win-x64          # 배포 빌드("빌드해줘"가 뜻하는 것)
```

- publish 결과: `src/CarrotIME/bin/publish/CarrotIME-1.0.0.N.exe` (self-contained 단일 exe, 압축, R2R off — 메모리 점유 때문. pubxml 주석 참고).
- 빌드 번호 `N`은 `src/CarrotIME/build.counter`(gitignore)가 빌드마다 자동 증가. AssemblyVersion은 1.0.0.0 고정.
- `src/**`가 main에 push되면 `.github/workflows/release.yml`이 테스트→publish→GitHub Release(v1.0.0.<run#>)를 자동 생성. 릴리즈 자산명은 `CarrotIME.exe`로 고정 — 자동 시작이 exe 절대경로를 등록하므로 바꾸면 안 된다.
- 커밋 메시지는 한국어 Conventional Commits(feat/fix/docs/ci/perf).

## 실기 검증 제약

앱이 관리자 권한으로 떠 있어 비관리자 셸에서는 프로세스 종료·재시작·모듈 조회가 안 된다.
동작 확인은 사용자에게 앱 재시작을 요청해야 한다. 설정은 `%APPDATA%\CarrotIME\settings.txt`, 시작 시 1회만 로드.

## 아키텍처

두 프로젝트로 분리되어 있고, 이 분리가 이 코드베이스의 핵심 규칙이다:

- **CarrotIME.Core** — OS 접근이 전혀 없는 순수 로직. 유일한 seam은 `Decider.Decide(InputSnapshot) → IndicatorView`: 표시 여부·라벨·위치 결정이 전부 이 순수 함수 뒤에 모인다. `CaretResolver`는 캐럿 소스 폴백 오케스트레이션(역시 순수 — 소스는 델리게이트 주입).
- **CarrotIME** — WinForms 셸. `IndicatorController`가 WinEvent 훅(포그라운드/포커스)으로 깨어나 어댑터로 `InputSnapshot`을 읽고 → `Decider.Decide` → `OverlayForm`에 반영. 편집 포커스가 있는 동안만 120ms 타이머로 한/영 토글을 폴링하고, 벗어나면 타이머 정지 + 워킹셋 트림(유휴 CPU·RAM ~0이 설계 목표).

**새 판정·위치 로직은 어댑터가 아니라 Decider(또는 Core)에 넣고 테스트를 먼저 쓴다.** 어댑터는 OS 사실 수집만 한다("넓은 rect는 캐럿이 아니다" 같은 판정도 Core 몫). 테스트는 Core만 존재하며 어댑터는 실기 확인에 의존한다.

### 캐럿 폴백 체인과 신뢰도

`CaretResolver`가 네이티브(GetGUIThreadInfo) → UIA(TextPattern selection) → MSAA(OBJID_CARET) 순으로 시도.
소스마다 `ImpliesEditable` 플래그가 다르다: 네이티브·MSAA 캐럿은 편집 필드에서만 존재하므로 편집 포커스 확정, **UIA 캐럿은 읽기 전용 텍스트(브라우저 본문 클릭)에서도 잡히므로 확정 근거가 못 된다** — 이 경우 `FocusInspector`로 재확인한다.

`FocusInspector` 판정 순서: ValuePattern 읽기 전용 → 거부, TextPattern의 IsReadOnly 속성이 bool로 확정 답을 주면 컨트롤 타입 무관하게 그걸 따름(role=combobox 입력·contenteditable 커버, 브라우저 본문·PDF 제외), 판정 불가면 Edit/Document 타입 화이트리스트 폴백.

### WPF 금지 (메모리 제약)

UIA는 관리형 `System.Windows.Automation`이 아니라 **COM(IUIAutomation)을 CsWin32로 직접 호출**한다. `UseWPF`를 켜면 WPF 스택이 번들·로드되어 메모리·exe가 크게 늘므로 금지(WS 121→8.6MB 최적화 이력). 새 Win32/UIA API가 필요하면 `src/CarrotIME/NativeMethods.txt`에 이름을 추가하면 CsWin32가 생성한다.

## 문서

- `docs/adr/` — 기술 선택 근거(캐럿 상주 표시, C#/.NET 선택, 관리자 권한 실행).
- `docs/specs/ime-caret-indicator.md` — 기능 스펙.
- README는 한국어가 기본, 영어판은 `docs/en/README.md`.
