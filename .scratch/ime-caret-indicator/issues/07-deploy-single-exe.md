# 07 — 배포: self-contained 단일 exe + 스모크

**What to build:** 최종 사용자가 .NET 런타임을 따로 설치하지 않고 실행 파일 하나(포터블)로 프로그램을 쓸 수 있다. 실행하면 UAC 승인 후 지금까지 만든 모든 동작(캐럿 추종·폴백·기타 IME·트레이·자동 시작)이 정상 작동한다.

**Blocked by:** 04, 05, 06

**Status:** ready-for-agent

- [ ] self-contained 단일 실행 파일이 생성된다
- [ ] .NET 미설치 환경에서 실행된다
- [ ] 실행 시 UAC 승인을 거쳐 관리자 권한으로 뜬다
- [ ] 캐럿 추종·고정 폴백·기타 IME·트레이·자동 시작이 배포본에서 모두 동작한다(스모크 체크리스트)

## 배포 방법

```
dotnet publish src/CarrotIME/CarrotIME.csproj -p:PublishProfile=win-x64
```
→ `src/CarrotIME/bin/publish/CarrotIME.exe` (단일 파일, self-contained, 약 76MB).
.NET 런타임 미설치 PC에서도 실행. 실행 시 UAC 승인 필요(관리자 권한 매니페스트 내장).

- self-contained 단일 exe, 트리밍 없음(WinForms/UIA 반사 안전), NativeAOT 미사용(WinForms 미지원).
- pdb·참조 부속 파일 미포함으로 exe 하나만 산출.
- 버전: 빌드마다 `build.counter` 증가 → FileVersion `1.0.0.N`(AssemblyVersion은 1.0.0.0 고정).

## 수동 스모크 체크리스트 (게시본으로)

- [ ] .NET 미설치(또는 다른) PC에서 exe 더블클릭 → UAC "예" → 실행됨
- [ ] 트레이에 파란 아이콘 + 현재 IME 글자(한/영) 표시
- [ ] 메모장 입력창 20초 유휴 → 캐럿 옆 인디케이터, 타이핑 시 사라짐
- [ ] 크롬 등 캐럿 미노출 앱 → 활성 창 우상단 폴백 표시
- [ ] 일본어/중국어 IME → あ/中
- [ ] 트레이: 일시정지/재개, 표시 지연 시간 변경, 종료 동작
- [ ] 자동 시작 켜기 → 재부팅 시 UAC 없이 자동 실행, 끄기 → 해제
- [ ] 멀티모니터·고DPI에서 크기·위치 정상
