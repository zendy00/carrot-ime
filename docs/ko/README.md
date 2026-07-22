# CarrotIME 🥕

[English](../../README.md) | **한국어**

윈도우 IME 표시기 — 입력 캐럿 우측에 현재 입력기(한 / A)를 표시합니다.

![캐럿 옆에 표시되는 CarrotIME 인디케이터](../images/screenshot.png)

## 소개

macOS의 입력 소스 표시기를 윈도우에서 재현합니다. 지금 입력기가 한글인지 영문인지(일본어·중국어 등 기타 IME도)를 텍스트 캐럿 바로 옆 작은 원으로 알려줍니다.

## 기능

- 캐럿 옆에 현재 입력 상태 표시 (한 / A / あ / 中).
- 입력 중에는 숨기고, 유휴하면 다시 표시 — 항상(0초)~30초 프리셋 또는 메뉴에서 바로 직접 입력(최대 599초). 기본 20초.
- 인디케이터 배경색·글자색 — 파스텔 팔레트 + 컬러 피커로 자유 지정.
- 트레이 아이콘에도 현재 상태 표시 (가 / A).
- 트레이 메뉴: 일시정지, 표시 지연 시간, 배경색, 글자색, Windows 시작 시 실행, 종료.
- 윈도우 다크 테마면 메뉴도 다크로 표시.

트레이 컨텍스트 메뉴:

![CarrotIME 트레이 컨텍스트 메뉴 (다크)](../images/menu.png)

## 빌드

```
dotnet publish src/CarrotIME/CarrotIME.csproj -p:PublishProfile=win-x64
```

→ `src/CarrotIME/bin/publish/CarrotIME-1.0.0.N.exe` — self-contained 단일 실행 파일. .NET 런타임 미설치 PC에서도 실행되며, 실행 시 관리자 권한(UAC) 승인이 필요합니다.
