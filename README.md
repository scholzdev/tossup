# Tossup (Unity port)

A native C# conversion of **Tossup** for **Unity 6.6 (6000.6.4f1)**. Gameplay, UI, rendering, audio, localization, and persistence run without a Lua runtime or Lua data files.

## Run it

- **Run in the Unity Editor:** `./tools/run_unity.sh`, then press Play.
- **Build macOS without launching:** `./tools/build_unity.sh macos`
- **Build Windows:** `./tools/build_unity.sh windows` (requires the Unity Windows build module)
- **Play an existing Windows build:** `Builds/Windows/Tossup.exe`
- **Open in Unity:** Unity Hub → *Add project from disk* → this folder → open `Assets/Scenes/Main.unity` → Play.

The scripts use the Unity version in `ProjectSettings/ProjectVersion.txt`. Set `UNITY_CLI=/path/to/unity` to use a nonstandard CLI install. To build from the command line, close the project in Unity first; if it is already open, use **Tossup → Build macOS Player** in the Editor. The macOS build is ad-hoc signed for local use; distribution builds still need a Developer ID signature and notarization.

Profiles and safe-point run saves use `profile.json` and `run.json` under `~/Library/Application Support/Tossup/Tossup` on macOS and `%USERPROFILE%\AppData\LocalLow\Tossup\Tossup` on Windows. Runs resume from the start of a level, the shop, or an augment choice. Developer runs use a separate `run-dev.json` and never change normal profile progress.

For development, launch the player with `-tossup-dev` (or set `TOSSUP_DEV=1`) to unlock content and start with 5,000 gold. A JSON sandbox scene can be loaded with `-tossup-sandbox Tools/sandbox/example.json`; press F5 to reload it after editing. `TOSSUP_SANDBOX_SCENE=/path/to/scene.json` provides the same startup behavior. Sandbox fields include `coins`, `character`, `stake`, `gold`, `energy`, `seed`, `screen`, and per-coin `odds` overrides.

## How the code maps to the original

| Original area | Unity C# area |
|---|---|
| Rules, RNG, events, hooks, relics, and chips | `Assets/Scripts/Core/` (`Game.cs`, `LatestSystems.cs`, `Rng.cs`, `Signal.cs`, `Hooks.cs`, `Model.cs`) |
| Coins, chips, relics, characters, and current catalogs | `Assets/Scripts/Content/Content.cs`, `LatestContent.cs` |
| Profile and progression | `Core/Profile.cs` + JSON saves |
| Localization | `UI/Lang.cs` + `Resources/locales/de.json` |
| App state, actions, drawing, sound, and screens | `UI/` and `UI/Views/` |
| `love.graphics` | `Render/Gfx.cs` (same immediate-mode API) drawn by `Render/UnityGfxBackend.cs` (GL) |
| Former application runtime | `Scripts/TossupApp.cs` (input, audio, saves, cursor, window) |

`Core`, `Content` and `UI` don't depend on UnityEngine. Only `TossupApp.cs` and `UnityGfxBackend.cs` do, so the headless tools can run the same C# game outside Unity. The views draw on a fixed 1280×800 canvas, scaled and letterboxed to the window. Baked font atlases and JSON metrics keep text placement deterministic.

## Verification tools (`Tools/`)

- `parity/csharp/`: runs deterministic seeded simulations directly against the C# rules.
- `uitest/`: checks the current gameplay catalogs and systems, draws every UI flow, runs random input stress, and plays deep runs (`dotnet run -c Release -- 300000 11`). Set `TOSSUP_SEED_SWEEP=100000` to simulate that many deterministic full runs across all characters and stakes.
- `.ecc/benchmarks/unity-vs-lua-rules.json`: the recorded five-sample rules benchmark used during conversion.

The native port also fixes the former Double Down/Echo effect crashes and the last-coin mouse soft-lock. Unity Personal shows its standard splash screen at startup.
