#!/bin/sh
# CarrotIME.app 번들 조립 — Xcode 없이 SwiftPM release 빌드 + 수동 번들 + ad-hoc 서명.
# 배포용 서명/공증(Developer ID)은 별도. 여기 ad-hoc은 로컬 실행용.
set -eu
cd "$(dirname "$0")"

CONFIG=release
APP="build/CarrotIME.app"
BIN=".build/${CONFIG}/CarrotIME"

echo "▸ swift build (${CONFIG})…"
swift build -c "${CONFIG}"

echo "▸ 앱 아이콘 생성(.iconset → .icns)…"
ICONSET="build/AppIcon.iconset"
".build/${CONFIG}/IconGen" "${ICONSET}"
iconutil -c icns "${ICONSET}" -o "build/AppIcon.icns"

echo "▸ .app 번들 조립…"
rm -rf "${APP}"
mkdir -p "${APP}/Contents/MacOS" "${APP}/Contents/Resources"
cp "${BIN}" "${APP}/Contents/MacOS/CarrotIME"
cp bundle/Info.plist "${APP}/Contents/Info.plist"
cp "build/AppIcon.icns" "${APP}/Contents/Resources/AppIcon.icns"

echo "▸ ad-hoc 코드서명…"
codesign --force --sign - "${APP}"

echo "✓ ${APP}"
echo "  실행:  open ${APP}"
echo "  자동시작(로그인 항목) 테스트는 /Applications 로 복사 후 실행 권장."
