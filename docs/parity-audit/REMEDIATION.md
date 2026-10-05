# Lua parity remediation

Reference: Lua `main@63a7c55`, baseline Unity `89df462`, verified 2026-10-05. The [original report](REPORT.md) and [evidence](evidence.json) remain the historical audit. Existing user edits were preserved. Checked items below mean the reported implementation gap is fixed; platform verification limits are listed separately.

| Finding | Implemented fix / evidence |
| --- | --- |
| [x] A1 | Encounter/augment imports use nearest filtering without mipmaps; existing metadata aligned. |
| [x] A2 | Resizable 1620×800 window (user-requested override of Lua’s original size) with minimum 640×400; native macOS build and player passed. |
| [x] A3 | Edge sound uses pitch 1.35; sound observation resets with the game instance. |
| [x] A4 | Shared version/build metadata, run diagnostics and timestamped crash log. Native player loads stamped metadata. |
| [x] G1 | Side bets settle once; focused repeated-flip regression. |
| [x] G2 | Emergency permission counts remaining coins; focused regression. |
| [x] G3 | Unpaid emergency energy costs up to 2g; focused regression. |
| [x] G4 | Echo copies effect duration and type; Whetstone and Megaphone regressions. |
| [x] G5 | Broken Clock protects final Heads from stage inversion; focused regression. |
| [x] G6 | Upgrade bonus follows the swapped effect side; focused regression. |
| [x] G7 | Type Specialist runs before Chaos duplication; focused regression. |
| [x] G8 | Cash Out resets zero-value combos; focused regression. |
| [x] G9 | Amazon Prime failure removes coins on level exit; focused regression. |
| [x] G10 | Shop odds exclude completed encounter hooks/contracts; focused regression. |
| [x] G11 | Upgrade starting reward uses the usable/unlocked pool; seeded reward-pool regression. |
| [x] G12 | Shop variant upgrade keys sorted ordinally before RNG selection; source verified. |
| [x] G13 | Returning an already-live UID cannot duplicate it; Mimic/Chaos regression. |
| [x] L1 | All 463 Lua flat German keys and all grouped locale entries copied exactly; exporter comparison passed. Modifier lookup connected. |
| [x] L2 | All 17 stale descriptions replaced with current Lua text; full exported catalog comparison passed. |
| [x] L3 | Reveal/augment source text translated before formatting or uppercase; German native screenshots inspected. |
| [x] P1 | Autosave only at untouched opening, shop or augment checkpoints; pending-flip regression. |
| [x] P2 | Completed shop history may reference removed UIDs; removal/resume regression. |
| [x] P3 | Bounded literal Lua save parser imports absent Unity profile/run files atomically without executing Lua or overwriting progress. Real Lua opening, shop/removal, pending augment and Unicode profile fixtures passed. |
| [x] P4 | Profile ranges, IDs, options and sets sanitized; malformed-profile regression. |
| [x] P5 | Rarity repair preserves later picks as Lua does; focused loadout regression. |
| [x] P6 | Restore validates IDs, choices, enum names, maps and numeric ranges; save regressions and full-run sweep passed. |
| [x] S1 | Sandbox odds combine with modifiers and clamp; focused regression. |
| [x] S2 | Sandbox shop uses restricted scene coin pool; focused regression. |
| [x] S3 | Initial resources and override odds affect quota estimation; Capacitor energy/odds regressions. |
| [x] S4 | Default, Blood and shop JSON scenes supplied; default environment mode and F5 reload connected. |
| [x] T1 | Linux build target, executable tarball and AppImage packaging implemented. Synthetic archive/permission and missing-tool checks passed; native Linux build requires its Unity module. |
| [x] T2 | Live-rule simulator supports all three bots, coin reports, seeds, stakes, unlocks, sets and route overrides; 57-coin report and seeded runs passed. |
| [x] T3 | All-target builds/packages and gated release workflow with stamping, notes, version/tag and publishing commands supplied. Gate-failure rollback checked in temporary fixtures for dry-run and publishing modes. Native macOS package passed. |
| [x] T4 | Docs/wiki use the C# live-content exporter. Docs check passed; 172 HTML and 173 Markdown wiki pages regenerated. |
| [x] T5 | GitHub Pages/Wiki workflow restored with .NET exporter and Unity input paths; remote execution pending. |
| [x] T6 | Parity bot uses current set limits, 11 chips, augment/contract actions and latest trace fields; 1,000-run trace without errors. |
| [x] U1 | Normal app flow saves untouched opening then automatically starts it and disables contract offers; UI tests passed. |
| [x] U2 | Central coin registers flip/advance action. |
| [x] U3 | Bank rows register coin details and clip long names. |
| [x] U4 | Tooltips show Heads/Edge/Tails, rarity, types, Fortune and upgrade details; native shop/upgrade screenshots inspected. |
| [x] U5 | Square Dance Heads explanation carried by its definition; native collection tooltip inspected. |
| [x] U6 | Wrapped dynamic rows, set-editor left anchor and encounter tooltip suppression. |
| [x] U7 | Shop shows owned chip/prize art with hovers; native screenshot inspected. |
| [x] U8 | Locked slots show padlocks and quota explanation; native screenshot inspected. |
| [x] U9 | Controller inspection associates focused Buy with artwork above it; scripted native inspection screenshot checked. |
| [x] U10 | Button hotkeys and connected-controller shortcut badges added; scripted native screenshot checked. |
| [x] U11 | Keyboard chip slots use the same zero-based index as mouse; key-1 regression. |
| [x] U12 | Edge uses 9.5 half-turns and a separate purple banner; native screenshot inspected. |
| [x] U13 | Reveal rays/rings, rotation and fade restored with affine image transforms; native screenshot inspected. |
| [x] Odds Tuner | Removed from shop; legacy core API retained for compatible tooling. |

## Coin definition architecture

All 57 coins have one complete concrete class under `Assets/Scripts/Content/Coins/`. `CoinDef` has overridden properties, enum types, typed effect/upgrade lists and virtual rule methods. Stateful quota estimates also belong to the coin class. Ordered catalogs, character decks/pools, shop offers and owned coins carry these definition objects. All 18 upgrades likewise have concrete definitions under `Content/Upgrades/`, referenced through `UpgradeCatalog`. Owned/shop upgrades carry those objects. String IDs remain at persistence, asset and localization boundaries. Version-1 saves still resolve the original ID format into shared objects.

`Heads`, `Tails` and `Edge` are independent lists because existing coins apply multiple effects. `Effect.HalfOf` is only a helper for existing Edge definitions; a coin may define arbitrary Edge effects. Adding an operation extends `EffectType`, its factory, the central effect executor and presentation text. The existing catalog and balance were preserved; illustrative Gold/Legendary content was not introduced.

The complete export before and after the refactor is identical after making the existing default cost of 15 explicit. Simulator summaries for 20 seeds × three characters × three bots (180 runs), and the current-system 1,000-run parity trace, were byte-identical across both definition refactors before the subsequent shop-purchase correction. The latter clears obsolete metadata and adds the Lua upgrade name to purchase logs.

## Follow-up fixes requested by the user

- Run encounters now reveal in developer mode as in Lua. The Unity host no longer forwards the same Start click as a dismissal: `Input.anyKeyDown` includes mouse buttons, so reveal dismissal now requires the reveal to have existed before that frame. Real normal/developer Start paths are tested, and native captures at two times demonstrate the animation.
- Default window changed to 1620×800. The design canvas and views now use the full 1620×800 width, with a half-width title menu, larger controls and coin art, and matching input, tutorial and controller coordinates. Resizing preserves aspect ratio; the title background covers the canvas.
- All standalone tool/test projects target .NET 10; `global.json` selects SDK 10. The wiki workflow installs SDK 10. Unity’s own runtime remains managed by Unity 6000.6.
- All 18 upgrades now have concrete files and shared objects, including owned/shop upgrade references. Save compatibility, wrong-coin rejection and object identity are tested.
- Buying an upgraded shop offer now clears its sold-slot upgrade metadata as Lua does. This also fixes a shop checkpoint rejection found while reviewing the new object path; the upgraded-purchase resume regression passes.
- Default builds/releases package macOS and Windows, with a native Windows PowerShell wrapper; Linux is optional.

## Verification

```sh
dotnet run --project Tools/uitest/UiTest.csproj -c Release --disable-build-servers -p:NuGetAudit=false -- 1000 11
dotnet run --project Tools/parity/csharp/Parity.csproj -c Release --disable-build-servers -p:NuGetAudit=false -- 1000 /tmp/tossup-complete-coins-parity.txt
sh Tools/game_tools.sh sim --runs 20 --bot all --char all
sh Tools/game_tools.sh sim --coins
sh Tools/build_docs.sh --check
sh Tools/build_wiki.sh
./Tools/build mac
Builds/macOS/Tossup.app/Contents/MacOS/Tossup -tossup-shots /tmp/tossup-unity-parity-shots -logFile /tmp/tossup-unity-parity-player.log
python3 Tools/package_unity.py macos
```

Focused gameplay/UI/save regressions, 1,000 complete-run seeds, 31-screen tour, 1,000 monkey frames and 1,000 player frames passed. The native macOS build and 31-shot capture completed without runtime exceptions. All 152 original assets still match their Lua SHA-256 hashes. German grouped entries and all 463 flat strings exactly match the Lua exporter. Shell syntax and Python parsing checks passed for the restored tooling.

## Remaining verification limits

- Windows Build Support (Mono) is now installed. `./tools/build windows` successfully cross-built the URP player on macOS; the output is a Windows x86-64 PE executable with its data/runtime files. Execution and visual checks on a Windows host remain outstanding. `./tools/build mac` builds macOS; `Tools/build_windows.ps1` provides native Windows build/package orchestration. Linux remains optional and is excluded from the default build/release targets at the user’s request. AppImage creation additionally requires `appimagetool`; fixture checks covered Windows ZIP, Linux executable tar permissions and clear missing-tool failure.
- Controller navigation/inspection was exercised through scripted UI state and a native screenshot. A physical-controller check remains outstanding.
- Release publishing and GitHub workflow execution were not run; no release tag or release was created by the parity gate. Gate failure and version rollback were tested without contacting remotes.
- The German table intentionally matches Lua. Lua itself lacks translations for Mathematician and its +10% Heads upgrade text, so those retain the same English fallback.

These fixes resolve the established audit gaps. They are not an exhaustive proof of equivalence for every possible interaction or platform.

- UI follow-up: centered and enlarged landing/applying feedback; the native system pointer keeps small hover targets visible. Layout regressions cover four window sizes, title background aspect ratio, pointer hit testing and screenshot-tour clickable bounds.

- Latest UI gate passed: 1,000 full-run seeds, 33 scripted screens, 1,000 monkey frames and 1,000 player frames. The earlier licensing blockage is resolved. Fresh URP macOS builds and a 33-screen native capture at 1620×800 completed without runtime exceptions, shader errors or Render Graph errors.

- Coin bank follow-up: size the panel around the remaining coins, with larger rows/icons for small banks and enough room for all ten. The footer, buffs, discard hint and tutorial spotlight follow its height. Layout checks cover every size from one to ten, shrinking after a flip, the empty state, and a full bank with buffs/discard controls.

- URP migration: URP 17.6 replaces the built-in pipeline. The immediate canvas API now collects reusable mesh batches, with separate properties for each texture/font/clip batch. `TossupCanvasFeature` draws through Render Graph after post processing; the shader uses URP HLSL and the render target's GPU projection. Fonts, clipping, transparency, rotated/flipping art and primitive order remain intact. Native captures inspected include title, gameplay, shop hover, collection, sets, German augment/upgrade choices, Edge, reveal animation and applying/resolved feedback. Screenshots: `/tmp/tossup-urp-shots`; native log: `/tmp/tossup-urp-player.log`.
- Pipeline and renderer assets are supplied with Graphics/all Quality levels connected. Running setup again reuses the same renderer feature and asset GUIDs; two successive macOS builds passed. Shader/build API mismatch during initial setup was corrected using Unity's Editor serialization API for URP's read-only runtime settings. No save/content migration is required. Windows rendering still requires verification on Windows.
