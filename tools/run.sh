#!/bin/sh
# Run the game.   ./tools/run.sh         start from the source folder with LÖVE (fast, shows LÖVE in the Dock)
#                 ./tools/run.sh app     build the binary for this OS and launch it:
#                                        macOS: dist/macos/Tossup.app, Linux: dist/linux/Tossup.AppImage,
#                                        Windows (Git Bash / MSYS): dist/windows/Tossup-windows/Tossup.exe
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
else
  exec love .
fi
