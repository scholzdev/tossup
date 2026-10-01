#!/bin/sh
# Build dist/linux/Tossup.AppImage: the official LÖVE 11.5 AppImage with the game appended (LÖVE runs a game fused to its AppImage).
# Needs curl; downloads the AppImage once (cached in dist/cache). Run it on Linux with `chmod +x Tossup.AppImage && ./Tossup.AppImage`.
# Not tested on a real Linux machine.   Run from anywhere:  ./tools/build_linux.sh
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
VERSION="${LOVE_VERSION:-11.5}"
APPIMAGE="love-$VERSION-x86_64.AppImage"
sh tools/build_love.sh >/dev/null
mkdir -p dist/cache dist/linux
[ -f "dist/cache/$APPIMAGE" ] || curl -fL -o "dist/cache/$APPIMAGE" "https://github.com/love2d/love/releases/download/$VERSION/$APPIMAGE"
cat "dist/cache/$APPIMAGE" dist/love/Tossup.love > dist/linux/Tossup.AppImage
chmod +x dist/linux/Tossup.AppImage
echo "built dist/linux/Tossup.AppImage"
