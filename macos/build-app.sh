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

echo "▸ 코드서명…"
SIGN_KC="carrotime-signing.keychain-db"
SIGN_CN="CarrotIME Local"
if security find-identity -p codesigning "${SIGN_KC}" 2>/dev/null | grep -q "${SIGN_CN}"; then
    security unlock-keychain -p carrotime "${SIGN_KC}" 2>/dev/null || true
    codesign --force --sign "${SIGN_CN}" "${APP}"
    echo "  (자체 서명 인증서 — 재빌드해도 접근성 권한 유지)"
else
    codesign --force --sign - "${APP}"
    echo "  (ad-hoc — 재빌드 시 접근성 재설정 필요. tools/make-signing-cert.sh 실행하면 안정화)"
fi

echo "✓ ${APP}"
echo "  실행:  open ${APP}"
echo "  자동시작(로그인 항목) 테스트는 /Applications 로 복사 후 실행 권장."
