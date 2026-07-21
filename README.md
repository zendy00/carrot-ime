# CarrotIME 🥕

**English** | [한국어](docs/ko/README.md)

Windows IME indicator — shows your current input source (한 / A) right next to the text caret.

![CarrotIME indicator shown next to the caret](docs/images/screenshot.png)

## About

A Windows port of the macOS input source indicator. A small badge next to the text caret shows whether you are typing Korean, English, or another IME (Japanese, Chinese, …).

## Features

- Shows the current input state next to the caret (한 / A / あ / 中).
- Hides while you type, reappears after an idle delay (default 20s).
- Tray icon also reflects the current state (가 / A).
- Tray menu: pause, reappear delay, indicator color palette, run at startup, exit.
- Menu follows the Windows dark theme.

Tray context menu:

![CarrotIME tray context menu (dark)](docs/images/menu.png)

## Build

```
dotnet publish src/CarrotIME/CarrotIME.csproj -p:PublishProfile=win-x64
```

→ `src/CarrotIME/bin/publish/CarrotIME-1.0.0.N.exe` — a self-contained single-file exe. It runs without a preinstalled .NET runtime and requires administrator (UAC) approval on launch.
