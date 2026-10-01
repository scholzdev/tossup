#!/bin/sh
# Run the game.   ./tools/run.sh         start from the source folder with LÖVE (fast, shows LÖVE in the Dock)
#                 ./tools/run.sh app     build dist/Tossup.app and open it (shows Tossup and the icon)
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
if [ "$1" = "app" ]; then
  sh tools/build_macos.sh
  open dist/Tossup.app
else
  exec love .
fi
