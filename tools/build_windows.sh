#!/bin/sh
# Build dist/windows/Tossup-windows.zip (and the unpacked dist/windows/Tossup-windows/): Tossup.exe (the game fused onto LÖVE's love.exe) plus the DLLs it needs.
# Needs curl, zip, unzip and node/npm (npm installs `resedit` once, to give the exe our icon and name); downloads the official LÖVE 11.5 64-bit Windows build (cached in dist/cache).
# Run from anywhere:  ./tools/build_windows.sh   then send dist/windows/Tossup-windows.zip to a Windows PC and run Tossup.exe
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
VERSION="${LOVE_VERSION:-11.5}"
ZIP="love-$VERSION-win64.zip"
mkdir -p dist/cache
sh tools/build_love.sh >/dev/null   # dist/love/Tossup.love (also makes assets/ui/icon.ico)
[ -f "dist/cache/$ZIP" ] || curl -fL -o "dist/cache/$ZIP" "https://github.com/love2d/love/releases/download/$VERSION/$ZIP"
OUT=dist/windows/Tossup-windows
rm -rf dist/windows
mkdir -p "$OUT"
unzip -q "dist/cache/$ZIP" -d dist/cache/win
cp dist/cache/win/love-$VERSION-win64/*.dll "$OUT"/
cp dist/cache/win/love-$VERSION-win64/license.txt "$OUT"/love-license.txt 2>/dev/null || true
cp LICENSE "$OUT"/LICENSE
# give love.exe our icon and name (resedit is installed once into dist/cache/node)
if [ ! -d dist/cache/node/node_modules/resedit ]; then
  mkdir -p dist/cache/node
  npm install --silent --prefix dist/cache/node resedit
fi
cp tools/set_exe_icon.mjs dist/cache/node/
( cd dist/cache/node && node set_exe_icon.mjs ../win/love-$VERSION-win64/love.exe ../../../assets/ui/icon.ico ../Tossup-love.exe Tossup )
# fusing = the exe followed by the .love archive; the result runs the game directly
cat dist/cache/Tossup-love.exe dist/love/Tossup.love > "$OUT/Tossup.exe"
rm -rf dist/cache/win
( cd dist/windows && zip -qr Tossup-windows.zip Tossup-windows )
echo "built dist/windows/Tossup-windows.zip"
