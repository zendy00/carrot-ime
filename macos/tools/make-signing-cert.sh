#!/bin/sh
# 로컬 코드서명용 자체 서명 인증서를 전용 키체인에 만든다(1회).
# ad-hoc 서명은 재빌드마다 cdhash가 바뀌어 접근성(TCC) 권한이 무효화되지만,
# 같은 인증서로 서명하면 designated requirement가 고정돼(= certificate leaf 해시)
# 재빌드해도 권한이 유지된다. Developer ID(유료)가 아니어도 로컬용으론 충분하다.
#
# 로그인 키체인/암호는 건드리지 않는다. 전용 키체인만 만든다.
# 되돌리기: security delete-keychain carrotime-signing.keychain-db
set -eu

KC="carrotime-signing.keychain-db"
PW="carrotime"          # 로컬 전용 키체인 암호(비밀 아님).
CN="CarrotIME Local"

if security find-identity -p codesigning "$KC" 2>/dev/null | grep -q "$CN"; then
    echo "✓ 이미 있음: $CN"
    exit 0
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
cd "$WORK"

security delete-keychain "$KC" 2>/dev/null || true
security create-keychain -p "$PW" "$KC"
security set-keychain-settings "$KC"          # 자동잠금 없음
security unlock-keychain -p "$PW" "$KC"

cat > csign.cnf <<EOF
[req]
distinguished_name = dn
x509_extensions = v3
prompt = no
[dn]
CN = $CN
[v3]
keyUsage = critical, digitalSignature
extendedKeyUsage = critical, codeSigning
basicConstraints = critical, CA:false
EOF

openssl req -x509 -newkey rsa:2048 -nodes -keyout k.pem -out c.pem -days 3650 -config csign.cnf
openssl pkcs12 -export -inkey k.pem -in c.pem -out c.p12 -passout pass:"$PW" -name "$CN"
security import c.p12 -k "$KC" -P "$PW" -T /usr/bin/codesign -A
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "$PW" "$KC" >/dev/null

# 코드사인이 찾도록 검색 목록에 추가(중복 방지).
EXISTING="$(security list-keychains -d user | sed 's/"//g' | xargs)"
case " $EXISTING " in
    *" $KC "*) : ;;
    *) security list-keychains -d user -s "$KC" $EXISTING ;;
esac

echo "✓ 생성: $CN (키체인 $KC). 이제 build-app.sh 가 이 인증서로 서명한다."
