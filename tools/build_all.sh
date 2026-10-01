#!/bin/sh
# Build every platform; each lands in dist/<platform>/:
#   dist/love/Tossup.love      dist/macos/Tossup.app      dist/windows/Tossup-windows.zip      dist/linux/Tossup.AppImage
# A platform that fails does not stop the others; the summary at the end says which worked.
# Run from anywhere:  ./tools/build_all.sh
cd "$(dirname "$0")/.."   # always work from the repo root
failed=""
for platform in love macos windows linux; do
  echo "== $platform"
  if ! sh "tools/build_$platform.sh"; then failed="$failed $platform"; fi
done
echo
echo "== result"
find dist/love dist/macos dist/windows dist/linux -maxdepth 1 \( -name "*.love" -o -name "*.app" -o -name "*.zip" -o -name "*.AppImage" \) 2>/dev/null | sort
[ -z "$failed" ] || { echo "FAILED:$failed"; exit 1; }
