#!/bin/sh
# Release is an explicit invocation. --dry-run runs all gates and packaging,
# produces notes/artifacts, and never commits, tags, pushes or publishes.
set -eu
cd "$(dirname "$0")/.."
exec python3 Tools/release.py "$@"
