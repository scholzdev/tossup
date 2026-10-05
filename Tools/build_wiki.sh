#!/bin/sh
# Build the local wiki into wiki/ (open wiki/index.html). It is generated from content/, locales/, assets/Resources and docs/, so it
# is always in step with the game. Needs .NET 10 and Python with Pillow.   Run from anywhere:  ./tools/build_wiki.sh
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
sh Tools/game_tools.sh content > dist-content.json
python3 Tools/gen_wiki.py dist-content.json
python3 Tools/gen_github_wiki.py dist-content.json wiki-github
rm -f dist-content.json
echo "open wiki/index.html"
