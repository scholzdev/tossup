#!/bin/sh
# Build the local wiki into wiki/ (open wiki/index.html). It is generated from content/, locales/, assets/Resources and docs/, so it
# is always in step with the game. Needs lua and Python with Pillow.   Run from anywhere:  ./tools/build_wiki.sh
set -e
cd "$(dirname "$0")/.."   # always work from the repo root
lua tools/dump_content.lua > dist-content.json
python3 tools/gen_wiki.py dist-content.json
python3 tools/gen_github_wiki.py dist-content.json wiki-github
rm -f dist-content.json
echo "open wiki/index.html"
