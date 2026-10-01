#!/bin/sh
# Publish a release: run the tests, build every platform, tag the version and attach the downloads to a GitHub release.
#   ./tools/build_all.sh release              release the version in src/version.lua
#   ./tools/build_all.sh release 0.3.0        set that version first (edits src/version.lua, commits and pushes it)
#   ./tools/build_all.sh release --dry-run    everything except the tag, the push and the release (files land in dist/release/)
#   --yes                                     do not ask before publishing
# Needs the GitHub CLI (`brew install gh`, then `gh auth login` once), a clean working tree on main, and everything build_all.sh needs.
set -e
cd "$(dirname "$0")/.."   # always work from the repo root

DRY=0; YES=0; NEW=""
for arg in "$@"; do
  case "$arg" in
    --dry-run) DRY=1 ;;
    --yes) YES=1 ;;
    [0-9]*.[0-9]*.[0-9]*) NEW="$arg" ;;
    *) echo "unknown argument: $arg"; exit 1 ;;
  esac
done

version() { sed -n 's/.*number = "\([^"]*\)".*/\1/p' src/version.lua; }

if [ "$DRY" = 0 ]; then
  command -v gh >/dev/null || { echo "GitHub CLI missing: brew install gh && gh auth login"; exit 1; }
  gh auth status >/dev/null 2>&1 || { echo "not logged in: gh auth login"; exit 1; }
  [ "$(git rev-parse --abbrev-ref HEAD)" = main ] || { echo "release from main only"; exit 1; }
fi
[ -z "$(git status --porcelain --untracked-files=no)" ] || { echo "working tree not clean: commit or stash first"; exit 1; }

if [ -n "$NEW" ] && [ "$NEW" != "$(version)" ]; then
  [ "$DRY" = 0 ] || { echo "dry run: not changing the version (would set $NEW)"; NEW=""; }
  if [ -n "$NEW" ]; then
    sed -i.bak "s/number = \"[^\"]*\"/number = \"$NEW\"/" src/version.lua && rm -f src/version.lua.bak
    git commit -q -am "Version $NEW"
  fi
fi
VERSION="$(version)"
TAG="v$VERSION"
if [ "$DRY" = 0 ]; then
  git fetch -q origin main --tags
  ! git rev-parse -q --verify "refs/tags/$TAG" >/dev/null || { echo "tag $TAG exists already: bump the version"; exit 1; }
  [ "$(git rev-parse HEAD)" = "$(git rev-parse origin/main)" ] || [ -n "$NEW" ] || { echo "main differs from origin/main: push or pull first"; exit 1; }
fi

echo "== tests"
for t in tests/test_*.lua; do lua "$t" | tail -1; done
./tools/build_docs.sh --check | tail -1

echo "== build"
./tools/build_all.sh

OUT=dist/release
rm -rf "$OUT"; mkdir -p "$OUT"
cp dist/love/Tossup.love "$OUT/Tossup-$VERSION.love"
cp dist/windows/Tossup-windows.zip "$OUT/Tossup-$VERSION-windows.zip"
cp dist/linux/Tossup.AppImage "$OUT/Tossup-$VERSION-linux.AppImage"
ditto -c -k --sequesterRsrc --keepParent dist/macos/Tossup.app "$OUT/Tossup-$VERSION-macos.zip"
echo "== files for $TAG"
ls -la "$OUT" | tail -n +2

NOTES="$OUT/notes.md"
cat > "$NOTES" <<NOTES_END
Tossup $VERSION

**Downloads**
- **Windows:** unpack \`Tossup-$VERSION-windows.zip\`, run \`Tossup.exe\`. Windows may warn about an unknown publisher: More info > Run anyway.
- **macOS:** unpack \`Tossup-$VERSION-macos.zip\`, then right-click \`Tossup.app\` > Open the first time (the app is not notarized). If macOS says it is damaged: \`xattr -dr com.apple.quarantine Tossup.app\`.
- **Linux:** \`chmod +x Tossup-$VERSION-linux.AppImage && ./Tossup-$VERSION-linux.AppImage\` (not tested on a real Linux machine).
- **Any system with LÖVE 11.5:** \`Tossup-$VERSION.love\`.

Educational use only, see the LICENSE file in the repository.

NOTES_END
PREVIOUS="$(git describe --tags --abbrev=0 2>/dev/null || true)"
echo "**Changes**" >> "$NOTES"
git log --no-merges --pretty='- %s' ${PREVIOUS:+"$PREVIOUS"..}HEAD >> "$NOTES"

if [ "$DRY" = 1 ]; then echo "dry run: stopped before tagging $TAG (notes in $NOTES)"; exit 0; fi

if [ "$YES" = 0 ]; then
  printf "Publish %s with these files on GitHub? [y/N] " "$TAG"
  read -r answer
  [ "$answer" = y ] || [ "$answer" = Y ] || { echo "cancelled; nothing was tagged or pushed"; exit 1; }
fi
git push -q origin main
git tag -a "$TAG" -m "Tossup $VERSION"
git push -q origin "$TAG"
gh release create "$TAG" "$OUT"/Tossup-"$VERSION"* --title "Tossup $VERSION" --notes-file "$NOTES" --verify-tag
echo "released $TAG"
