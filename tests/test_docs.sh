#!/bin/sh
# Docs must match the game's data (run ./tools/build_docs.sh to fix).
cd "$(dirname "$0")/.." && ./tools/build_docs.sh --check
