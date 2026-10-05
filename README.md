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

Saves use `profile.json` under `~/Library/Application Support/Tossup/Tossup` on macOS and `%USERPROFILE%\AppData\LocalLow\Tossup\Tossup` on Windows. On first start, Unity can read the original edition's legacy save and immediately writes a JSON copy, so existing progress carries over. The original save is only read, never changed.

## How the code maps to the original

| Original (Lua) | Port (C#) |
|---|---|
| `src/game.lua`, `rng.lua`, `signal.lua`, `hooks.lua`, `relics.lua`, `items.lua` | `Assets/Scripts/Core/` (`Game.cs`, `Rng.cs`, `Signal.cs`, `Hooks.cs`, `Model.cs`) |
| `content/**` (coins, items, relics, characters) | `Assets/Scripts/Content/Content.cs` |
| `src/profile.lua` | `Core/Profile.cs` + JSON saves; `LegacyProfileData.cs` only imports an existing save once |
| `src/lang.lua`, `locales/de.lua` | `UI/Lang.cs` + `Resources/locales/de.json` |
| `src/ui/app.lua`, `state.lua`, `actions.lua`, `draw.lua`, `sound.lua`, `theme.lua` | `UI/AppCore.cs`, `UiState.cs`, `Actions.cs`, `Draw.cs`, `Sound.cs`, `Theme.cs` |
| `src/ui/views/*.lua` | `UI/Views/EncounterView.cs`, `ShopView.cs`, `MenuViews.cs` |
| `love.graphics` | `Render/Gfx.cs` (same immediate-mode API) drawn by `Render/UnityGfxBackend.cs` (GL) |
| `main.lua` / LÖVE runtime | `Scripts/TossupApp.cs` (input, audio, saves, cursor, window) |

`Core`, `Content` and `UI` don't depend on UnityEngine. Only `TossupApp.cs` and `UnityGfxBackend.cs` do, so the headless tools can run the same C# game outside Unity. The views draw on a fixed 1280×800 canvas, scaled and letterboxed to the window. Baked font atlases and JSON metrics keep text placement deterministic.

## Verification tools (`Tools/`)

- `parity/csharp/`: runs deterministic seeded simulations directly against the C# rules.
- `uitest/`: runs the real UI code headless: the screenshot tour, a random "monkey" session, and a player that clicks through deep runs (`dotnet run -c Release -- 300000 11`).
- `.ecc/benchmarks/unity-vs-lua-rules.json`: the recorded five-sample rules benchmark used during conversion.

## Deliberate differences from the original

- **Crash fixes.** The original game crashes when Double Down is used on a coin whose effects include "next coin lands Heads" or "next coin swaps sides" (Domino, Mirror). It also crashes when Echo repeats a "next coins" buff (after Megaphone or Cheerleader). The port plays those situations out instead.
- **No mouse soft-lock.** After the last coin of a stack, the original greys out NEXT COIN and only Space continues. In the port the button works like Space.
- **Language is remembered.** The original wrote the language option unquoted, so German reset to English on every start. Unity stores it in JSON and still imports old saves.
- The game no longer moves your mouse cursor to the window corner on start.
- Unity Personal shows the "Made with Unity" splash screen at startup.
