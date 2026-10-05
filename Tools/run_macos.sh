#!/bin/sh
set -eu

ROOT=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
APP="$ROOT/Builds/macOS/Tossup.app"

if [ "$(uname -s)" != Darwin ]; then
  echo "This script must be run on macOS." >&2
  exit 1
fi
if ! command -v open >/dev/null 2>&1; then
  echo "macOS 'open' command not found." >&2
  exit 127
fi

"$ROOT/Tools/build" mac
echo "Launching $APP"
open "$APP"
