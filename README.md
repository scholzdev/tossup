# Tossup (Unity port)

A native C# conversion of **Tossup** for **Unity 6.6 (6000.6.4f1)** with **URP 17.6**. Gameplay, UI, rendering, audio, localization, and persistence run without a Lua runtime or Lua data files.

## Run it

- **Run in the Unity Editor:** `./Tools/run_unity.sh`, then press Play.
- **Build and launch a native player:** `./Tools/run mac|windows|linux` (use the matching OS; Linux requires its Unity module).
- **Build macOS without launching:** `./tools/build mac` → `Builds/macOS/Tossup.app`.
- **Build Windows from macOS:** `./tools/build windows` → `Builds/Windows/Tossup.exe`. Install Windows Build Support (Mono) for Unity 6000.6.4f1; copy the complete `Builds/Windows` folder to Windows, or package it with `python3 Tools/package_unity.py windows`.
- **Build/package on Windows:** `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build_windows.ps1` (or the **Tossup → Build Windows Player** Editor menu). Both paths require Windows Build Support installed for Unity 6000.6.4f1.
- **Package a player:** `python3 Tools/package_unity.py macos|windows|linux`; Linux AppImage packaging requires `appimagetool` or `APPIMAGETOOL`.
- **Build/package macOS and Windows:** `./Tools/build_all.sh`
- **Check a release locally:** `./Tools/build_all.sh release --dry-run`; normal `release` runs the same gates, prepares notes, tags and publishes after confirmation.
- **Play an existing Windows build:** `Builds/Windows/Tossup.exe`
- **Open in Unity:** Unity Hub → *Add project from disk* → this folder → open `Assets/Scenes/Main.unity` → Play.

The scripts use the Unity version in `ProjectSettings/ProjectVersion.txt`. Set `UNITY_EDITOR=/path/to/Unity` for a nonstandard build Editor, or `UNITY_CLI=/path/to/unity` for the launch CLI. To build from the command line, close the project in Unity first; if it is already open, use **Tossup → Build macOS Player** in the Editor. The macOS build is ad-hoc signed for local use; distribution builds still need a Developer ID signature and notarization.

Existing LÖVE `tossup` saves are imported automatically on the first normal launch when Unity saves are absent. Originals and existing Unity saves are preserved. `TOSSUP_LEGACY_SAVE_DIR` can point at another Lua save folder.

Profiles and safe-point run saves use `profile.json` and `run.json` under `~/Library/Application Support/Tossup/Tossup` on macOS and `%USERPROFILE%\AppData\LocalLow\Tossup\Tossup` on Windows. Runs resume from the start of a level, the shop, or an augment choice. Developer runs use a separate `run-dev.json` and never change normal profile progress.

Run encounters and their animated reveal appear in both normal and developer runs, matching Lua. Sandbox scenes and the tutorial retain their deliberate encounter-free flow.

For development, launch the player with `-tossup-dev` (or set `TOSSUP_DEV=1`) to unlock content and start with 5,000 gold. `TOSSUP_SANDBOX=1` starts the default scene; Blood and shop fixtures are in `Tools/sandbox/`. A JSON sandbox scene can be loaded with `-tossup-sandbox Tools/sandbox/example.json`; press F5 to reload it after editing. `TOSSUP_SANDBOX_SCENE=/path/to/scene.json` provides the same startup behavior. Sandbox fields include `coins`, `character`, `stake`, `gold`, `energy`, `seed`, `screen`, and per-coin `odds` overrides.

## How the code maps to the original

| Original area | Unity C# area |
|---|---|
| Rules, RNG, events, hooks, relics, and chips | `Assets/Scripts/Core/` (`Game.cs`, `LatestSystems.cs`, `Rng.cs`, `Signal.cs`, `Hooks.cs`, `Model.cs`) |
| One complete definition per coin | `Assets/Scripts/Content/Coins/*Coin.cs`, with `CoinDef.cs` as the base and `CoinCatalog.cs` holding ordered objects |
| Reusable upgrades | `Assets/Scripts/Content/Upgrades/*Upgrade.cs`, `Upgrade.cs` and `UpgradeCatalog.cs` |
| Chips, relics and characters | `Assets/Scripts/Content/Content.cs`, `LatestContent.cs` |
| Profile and progression | `Core/Profile.cs` + JSON saves |
| Localization | `UI/Lang.cs` + `Resources/locales/de.json` |
| App state, actions, drawing, sound, and screens | `UI/` and `UI/Views/` |
| `love.graphics` | `Render/Gfx.cs` (same immediate-mode API) drawn by `Render/UnityGfxBackend.cs` (mesh batches) and `Render/TossupCanvasFeature.cs` (URP Render Graph) |
| Former application runtime | `Scripts/TossupApp.cs` (input, audio, saves, cursor, window) |

`Core`, `Content` and `UI` don't depend on UnityEngine. The Unity host and files under `Render` other than `Gfx.cs` do, so the headless tools can run the same C# game outside Unity. The default window is 1620×800. The views use the full 1620×800 design canvas, with a title menu occupying half its width and wider gameplay/shop layouts. Resizing scales the canvas uniformly; title artwork covers it without stretching. Baked font atlases and JSON metrics keep text placement deterministic.

The URP assets in `Assets/Settings` are assigned in Graphics and every Quality level. The canvas runs after post processing through a Render Graph pass, preserving primitive order, clipping, transparency and pixel art colors in Gamma space. **Tossup → Set Up Project** recreates missing pipeline assets and reconnects them; command-line builds run the same setup. Saves and content definitions have not changed.

## Coin definitions

Each coin overrides its data properties, typed side-effect lists, and any special rule methods in one class. `Rarity`, `CoinType`, `EffectType` and `UpgradeType` are enums. `Heads`, `Tails` and `Edge` can each contain several effects, authored with factories such as `Effect.Score(10)` and `Effect.Quota(6)`. `Effect.HalfOf` is a helper for the current Edge rules; each coin can override Edge with any effect list. Override `OnResolve`, `OnFlip`, `OnOdds`, `OnDeal`, `OnDiscard`, `Grow` or `Register` for coin-specific rules; stateful quota estimates belong in `EstimateExtraScore`, and per-run counters remain on `CoinInst`.

Add a class in `Content/Coins/` and its object in `CoinCatalog`. Character decks/pools, shop offers and owned coins carry these objects. Upgrades follow the same pattern: each has a concrete class in `Content/Upgrades/`, and coin definitions reference `UpgradeCatalog` objects. Owned and shop upgrades carry those objects too. Stable string IDs are retained at save, localization and asset boundaries. Add a new effect operation to `EffectType`, its factory in `Effect`, its behavior in `Game.ApplyEffect`, and its text in `D.Effects` / `D.EffectDescription`; saving and content export derive the stable effect key from the enum. New enum values without implemented behavior fail explicitly.

## Verification tools (`Tools/`)

The three standalone projects target **.NET 10**, with SDK selection in `global.json`. Unity 6000.6 uses its own Mono/.NET Standard-compatible runtime; installing .NET 10 does not change the Unity player runtime.

- `game_tools.sh content`: exports the live C# catalog for docs/wiki generation.
- `game_tools.sh sim`: balance bots, per-coin reports (`--coins`), seeded logs (`--trace 7`), stake, unlock, set, quota and payout overrides.
- `build_docs.sh --check`: verifies generated documentation; `build_wiki.sh` generates English/German Pages and GitHub Wiki outputs.
- `parity/csharp/`: runs deterministic seeded simulations directly against the C# rules.
- `uitest/`: checks the current gameplay catalogs and systems, draws every UI flow, runs random input stress, and plays deep runs (`dotnet run -c Release -- 300000 11`). Set `TOSSUP_SEED_SWEEP=100000` to simulate that many deterministic full runs across all characters and stakes.
- `.ecc/benchmarks/unity-vs-lua-rules.json`: the recorded five-sample rules benchmark used during conversion.

The native port also fixes the former Double Down/Echo effect crashes and the last-coin mouse soft-lock. Unity Personal shows its standard splash screen at startup.
