# Unity migration status

The Unity runtime is fully native C#. `Assets`, `Packages`, `ProjectSettings`, and `Tools` contain no Lua files. Localization, font metrics, and new saves use JSON. A small C# compatibility reader imports an existing `profile.lua` once and writes `profile.json`; no Lua code is executed or shipped.

## Completed

- Unity 6.6 project and arm64 macOS build
- C# rules, UI, rendering, input, audio, localization, and persistence for the established 43-coin edition
- All current images, fonts, music, and sound effects moved into Unity Resources with Unity metadata
- Full original Git history preserved on the `unity-6` branch
- Deterministic parity and headless UI validation

## Current `main` gameplay still to port

- 15 newer coin definitions and their mechanics (58 current images versus 43 registered C# coins)
- 4 newer consumable definitions (11 current images versus 7 registered C# items)
- Eight-stage route, stakes, modifiers, ties, coin upgrades, and type synergies
- Combo banking/push decisions and side bets
- Contracts, run encounters, augments, and their selection/result screens
- Updated profile progression and German strings for those systems

The newer assets are already present in `Assets/Resources`; they remain unused until their C# rules and UI flows are implemented and parity-tested.
