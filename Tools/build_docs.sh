#!/bin/sh
# Rewrite the data tables of the docs from the game's content (coins, chips, prizes, modifiers, levels, characters).
#   ./tools/build_docs.sh           update the docs
#   ./tools/build_docs.sh --check   change nothing, fail if a doc is out of date or a coin has no section
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
sh Tools/game_tools.sh content > dist-content.json
python3 Tools/gen_docs.py dist-content.json "$@" || { rm -f dist-content.json; exit 1; }
rm -f dist-content.json
