#!/bin/sh
set -eu

ROOT=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
TARGET=${1:-macos}
VERSION=$(sed -n 's/^m_EditorVersion: //p' "$ROOT/ProjectSettings/ProjectVersion.txt")
UNITY_EDITOR=${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}

case "$TARGET" in
  macos)
    CLI_TARGET=StandaloneOSX
    METHOD=Tossup.EditorTools.TossupBuild.BuildMacOS
    OUTPUT="$ROOT/Builds/macOS/Tossup.app"
    ;;
  windows)
    CLI_TARGET=StandaloneWindows64
    METHOD=Tossup.EditorTools.TossupBuild.BuildWindows
    OUTPUT="$ROOT/Builds/Windows/Tossup.exe"
    ;;
  *)
    echo "usage: $0 [macos|windows]" >&2
    exit 2
    ;;
esac

if [ ! -x "$UNITY_EDITOR" ]; then
  echo "Unity Editor not found at $UNITY_EDITOR. Set UNITY_EDITOR to the Editor executable." >&2
  exit 127
fi

mkdir -p "$ROOT/Builds"
LOG="$ROOT/Builds/unity-build-$TARGET.log"
LOCK="$ROOT/Temp/UnityLockfile"
if [ -e "$LOCK" ]; then
  if ! RUNNING_EDITORS=$(ps -axo pid=,command=); then
    echo "Could not check whether Unity has this project open; refusing to remove $LOCK." >&2
    exit 1
  fi
  if printf '%s\n' "$RUNNING_EDITORS" | awk -v root="$ROOT" 'index($0, root) && $0 ~ /Unity\.app\/Contents\/MacOS\/Unity([[:space:]]|$)/ { found=1 } END { exit !found }'; then
    echo "Unity has this project open. Build from the Editor using Tossup > Build macOS Player, or close Unity before running this script." >&2
    exit 1
  fi
  echo "Removing stale Unity project lock at $LOCK."
  rm -f "$LOCK"
fi

echo "Building $CLI_TARGET with Unity $VERSION... (logs: $LOG)"
if ! "$UNITY_EDITOR" -batchmode -nographics -quit \
  -projectPath "$ROOT" -buildTarget "$CLI_TARGET" \
  -executeMethod "$METHOD" -buildOutput "$OUTPUT" -logFile "$LOG"; then
  echo "Unity build failed; log: $LOG" >&2
  tail -n 60 "$LOG" >&2 || true
  exit 1
fi

if [ "$TARGET" = macos ] && [ ! -d "$OUTPUT" ]; then
  echo "Unity exited without creating $OUTPUT; log: $LOG" >&2
  tail -n 60 "$LOG" >&2 || true
  exit 1
fi
if [ "$TARGET" = windows ] && [ ! -f "$OUTPUT" ]; then
  echo "Unity exited without creating $OUTPUT; log: $LOG" >&2
  tail -n 60 "$LOG" >&2 || true
  exit 1
fi

echo "Built $OUTPUT"
