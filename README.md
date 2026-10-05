# Tossup (Unity port)

A native C# conversion of **Tossup** for **Unity 6.6 (6000.6.4f1)**. Gameplay, UI, rendering, audio, localization, and persistence run without a Lua runtime or Lua data files.

## Run it

- **Play the macOS build:** `open Builds/macOS/Tossup.app`
- **Play the Windows build:** `Builds/Windows/Tossup.exe`
- **Open in Unity:** Unity Hub → *Add project from disk* → this folder → open `Assets/Scenes/Main.unity` → Play.
- **Build again:** use **Tossup → Build macOS Player** in the editor, or run:

  ```sh
  unity build --target StandaloneOSX \
    --execute-method Tossup.EditorTools.TossupBuild.BuildMacOS \
    --editor-version 6000.6.4f1 --architecture arm64 \
    --no-provenance .
  ```

  The build method ad-hoc signs local macOS builds. Distribution builds still need a Developer ID signature and notarization.

Saves use `profile.json` under `~/Library/Application Support/Tossup/Tossup` on macOS and `%USERPROFILE%\AppData\LocalLow\Tossup\Tossup` on Windows.

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
- `uitest/`: checks the current gameplay catalogs and systems, draws every UI flow, runs random input stress, and plays deep runs (`dotnet run -c Release -- 300000 11`).
- `.ecc/benchmarks/unity-vs-lua-rules.json`: the recorded five-sample rules benchmark used during conversion.

The native port also fixes the former Double Down/Echo effect crashes and the last-coin mouse soft-lock. Unity Personal shows its standard splash screen at startup.
