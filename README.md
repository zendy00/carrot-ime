# CarrotIME 🥕

윈도우 IME 표시기 (입력 캐럿 우측에 현재 IME를 표시합니다)

Windows IME indicator — shows your current input source (한/A) right next to the text caret.

![CarrotIME 인디케이터가 캐럿 옆에 표시되는 모습 / indicator next to the caret](docs/images/screenshot.png)

## 소개 / About

macOS의 입력 소스 표시기를 윈도우에서 재현합니다. 지금 입력기가 한글인지 영문인지(일본어·중국어 등 기타 IME도)를 텍스트 캐럿 바로 옆 작은 원으로 알려줍니다.

A Windows port of the macOS input source indicator. A small badge next to the text caret shows whether you are typing Korean, English, or another IME (Japanese, Chinese, …).

## 기능 / Features

- 캐럿 옆에 현재 입력 상태 표시 (한 / A / あ / 中). / Shows input state next to the caret.
- 입력 중에는 숨기고, 일정 시간(기본 20초) 유휴하면 표시. / Hides while typing, reappears after an idle delay (default 20s).
- 트레이 아이콘에도 현재 상태 표시(가 / A). / Tray icon also reflects the current state.
- 트레이 메뉴: 일시정지, 표시 지연 시간, 인디케이터 색상 팔레트, Windows 시작 시 실행, 종료. / Tray menu: pause, idle delay, color palette, run at startup, quit.
- 윈도우 다크 테마면 메뉴도 다크로 표시. / Menu follows the Windows dark theme.

트레이 컨텍스트 메뉴 / Tray context menu:

![CarrotIME 트레이 컨텍스트 메뉴 (다크) / tray context menu (dark)](docs/images/menu.png)

## 빌드 / Build

```
dotnet publish src/CarrotIME/CarrotIME.csproj -p:PublishProfile=win-x64
```

→ `src/CarrotIME/bin/publish/CarrotIME-1.0.0.N.exe` — self-contained 단일 실행 파일. .NET 런타임 미설치 PC에서도 실행되며, 실행 시 관리자 권한(UAC) 승인이 필요합니다. / A self-contained single-file exe; runs without a preinstalled .NET runtime and requires admin (UAC) approval on launch.
