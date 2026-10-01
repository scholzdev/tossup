#!/bin/sh
# Build dist/love/Tossup.love: the game as a plain LÖVE archive (runs on any machine with LÖVE: `love Tossup.love`).
# The platform builds reuse it.   Run from anywhere:  ./tools/build_love.sh
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
python3 tools/gen_app_icon.py >/dev/null   # makes assets/ui/icon.icns and icon.ico
mkdir -p dist/love
rm -f dist/love/Tossup.love
zip -qr dist/love/Tossup.love main.lua conf.lua src content locales assets -x "*.DS_Store"
echo "built dist/love/Tossup.love"
