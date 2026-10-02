#!/bin/sh
# Run the game.   ./tools/run.sh         start from the source folder with LÖVE (fast, shows LÖVE in the Dock)
#                 ./tools/run.sh app     build the binary for this OS and launch it:
#                                        macOS: dist/macos/Tossup.app, Linux: dist/linux/Tossup.AppImage,
#                                        Windows (Git Bash / MSYS): dist/windows/Tossup-windows/Tossup.exe
#                 ./tools/run.sh scenes/shop.lua  launch a sandbox scene file; F5 reloads it
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
if [ "$1" = "app" ]; then
  case "$(uname -s)" in
    Darwin)
      sh tools/build_macos.sh
      open dist/macos/Tossup.app ;;
    Linux)
      sh tools/build_linux.sh
      exec dist/linux/Tossup.AppImage ;;
    MINGW*|MSYS*|CYGWIN*)
      sh tools/build_windows.sh
      exec dist/windows/Tossup-windows/Tossup.exe ;;
    *)
      echo "unknown OS: $(uname -s)"; exit 1 ;;
  esac
elif [ -n "$1" ]; then
  case "$1" in
    *.lua) ;;
    *) echo "expected a project-relative .lua scene file" >&2; exit 1 ;;
  esac
  if [ ! -f "$1" ]; then echo "scene file not found: $1" >&2; exit 1; fi
  export SANDBOX=1 SANDBOX_SCENE="$1"
  exec love .
else
  exec love .
fi
