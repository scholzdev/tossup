#!/bin/sh
set -eu

ROOT=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
VERSION=$(sed -n 's/^m_EditorVersion: //p' "$ROOT/ProjectSettings/ProjectVersion.txt")
UNITY_CLI=${UNITY_CLI:-unity}
if ! command -v "$UNITY_CLI" >/dev/null 2>&1; then
  echo "Unity CLI not found. Install it or set UNITY_CLI to its executable path." >&2
  exit 127
fi

exec "$UNITY_CLI" --no-banner open "$ROOT" --editor-version "$VERSION"
