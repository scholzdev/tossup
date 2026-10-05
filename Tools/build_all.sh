#!/bin/sh
# Build/package the current macOS and Windows targets; report all failures.
set -u
cd "$(dirname "$0")/.." || exit 1
if [ "${1:-}" = release ]; then shift; exec sh Tools/release.sh "$@"; fi
if [ "$#" != 0 ]; then echo "usage: $0 [release [version] [--dry-run] [--yes]]" >&2; exit 2; fi
failed=""
for platform in macos windows; do
  if sh Tools/build_unity.sh "$platform" && python3 Tools/package_unity.py "$platform"; then
    echo "$platform: built and packaged"
  else
    failed="$failed $platform"
  fi
done
if [ -n "$failed" ]; then echo "FAILED:$failed" >&2; exit 1; fi
