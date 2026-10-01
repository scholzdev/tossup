#!/bin/sh
# Build dist/Tossup-windows.zip: Tossup.exe (the game fused onto LÖVE's love.exe) plus the DLLs it needs.
# Needs curl, zip and unzip; downloads the official LÖVE 11.5 64-bit Windows build (cached in dist/cache).
# Run from anywhere:  ./tools/build_windows.sh   then send dist/Tossup-windows.zip to a Windows PC and run Tossup.exe
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
VERSION="${LOVE_VERSION:-11.5}"
ZIP="love-$VERSION-win64.zip"
mkdir -p dist/cache
[ -f "dist/cache/$ZIP" ] || curl -fL -o "dist/cache/$ZIP" "https://github.com/love2d/love/releases/download/$VERSION/$ZIP"
OUT=dist/Tossup-windows
rm -rf "$OUT" dist/Tossup-windows.zip dist/Tossup.love
mkdir -p "$OUT"
unzip -q "dist/cache/$ZIP" -d dist/cache/win
cp dist/cache/win/love-$VERSION-win64/*.dll "$OUT"/
cp dist/cache/win/love-$VERSION-win64/license.txt "$OUT"/love-license.txt 2>/dev/null || true
zip -qr dist/Tossup.love main.lua conf.lua src content locales assets -x "*.DS_Store"
# fusing = love.exe followed by the .love archive; the result runs the game directly
cat dist/cache/win/love-$VERSION-win64/love.exe dist/Tossup.love > "$OUT/Tossup.exe"
rm -rf dist/cache/win
( cd dist && zip -qr Tossup-windows.zip Tossup-windows )
echo "built dist/Tossup-windows.zip"
