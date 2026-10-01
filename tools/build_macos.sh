#!/bin/sh
# Build dist/Tossup.app: a copy of LÖVE (/Applications/love.app) named Tossup, with our icon and the game inside.
# The Dock and the app switcher then show "Tossup" and the coin icon instead of LÖVE. Needs the LÖVE app and `zip`.
# Run from the repo root:  sh tools/build_macos.sh   then   open dist/Tossup.app
set -e
LOVE_APP="${LOVE_APP:-/Applications/love.app}"
[ -d "$LOVE_APP" ] || { echo "LÖVE not found at $LOVE_APP (set LOVE_APP)"; exit 1; }
python3 tools/gen_app_icon.py >/dev/null   # makes assets/ui/icon.icns
APP=dist/Tossup.app
rm -rf "$APP"
mkdir -p dist
cp -R "$LOVE_APP" "$APP"
RES="$APP/Contents/Resources"
# the game, as a .love archive that LÖVE picks up from its Resources folder
zip -qr "$RES/Tossup.love" main.lua conf.lua src content locales assets -x "*.DS_Store"
# icon: replace LÖVE's (the asset catalog entry would win over the file, so remove it)
cp assets/ui/icon.icns "$RES/OS X AppIcon.icns"
cp assets/ui/icon.icns "$RES/Tossup.icns"
rm -f "$RES/Assets.car"
PLIST="$APP/Contents/Info.plist"
set_plist() { /usr/libexec/PlistBuddy -c "Set :$1 $2" "$PLIST" 2>/dev/null || /usr/libexec/PlistBuddy -c "Add :$1 string $2" "$PLIST"; }
set_plist CFBundleName Tossup
set_plist CFBundleDisplayName Tossup
set_plist CFBundleIdentifier com.tossup.game
set_plist CFBundleIconFile Tossup
set_plist CFBundleShortVersionString 1.0
set_plist CFBundleVersion 1
/usr/libexec/PlistBuddy -c "Delete :CFBundleIconName" "$PLIST" 2>/dev/null || true
/usr/libexec/PlistBuddy -c "Delete :UTExportedTypeDeclarations" "$PLIST" 2>/dev/null || true
/usr/libexec/PlistBuddy -c "Delete :CFBundleDocumentTypes" "$PLIST" 2>/dev/null || true
# modified bundles must be signed again (ad hoc is enough to run on this machine)
codesign --force --deep --sign - "$APP" >/dev/null 2>&1 || echo "codesign failed: the app may be blocked by Gatekeeper"
echo "built $APP"
