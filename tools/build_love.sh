#!/bin/sh
# Build dist/love/Tossup.love: the game as a plain LÖVE archive (runs on any machine with LÖVE: `love Tossup.love`).
# The platform builds reuse it.   Run from anywhere:  ./tools/build_love.sh
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
python3 tools/gen_app_icon.py >/dev/null   # makes assets/ui/icon.icns and icon.ico
mkdir -p dist/love
rm -f dist/love/Tossup.love
# stamp the git commit into the build (shown on the title screen, written to runs.log and crash.log)
BUILD="$(git rev-parse --short HEAD 2>/dev/null || echo dev)"
[ -n "$(git status --porcelain --untracked-files=no 2>/dev/null)" ] && BUILD="$BUILD-dirty"   # uncommitted changes in tracked files
echo "return \"$BUILD\"" > src/build_id.lua
# the icons and the unused title background only matter for the platform builds, not inside the .love; the LICENSE travels with every build
zip -qr dist/love/Tossup.love main.lua conf.lua LICENSE src content locales assets -x "*.DS_Store" "assets/ui/title_background.png" "assets/ui/icon.icns" "assets/ui/icon.ico"
rm -f src/build_id.lua
echo "built dist/love/Tossup.love"
