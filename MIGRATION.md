# Unity migration status

The Unity runtime is fully native C#. `Assets`, `Packages`, `ProjectSettings`, and `Tools` contain no script files from the former runtime. Localization, font metrics, profiles, and progression use JSON.

## Completed

- Unity 6.6 project and arm64 macOS build
- C# rules and UI for all 57 coins, 11 chips, eight stages, coin upgrades, types, ties, deck slots, stakes, and level modifiers
- Combo banking and pushing, side bets, contracts, run encounters, augments, and their selection screens
- Character, stake, and endless progression stored in the JSON profile
- Safe-point run saving and resume, the guided 18-step tutorial, developer mode, JSON sandbox scenes, and the animated run-encounter reveal
- Native rendering, input, audio, localization, and persistence
- All current images, fonts, music, and sound effects moved into Unity Resources with Unity metadata
- Full original Git history preserved on the `unity-6` branch
- Focused current-gameplay checks, deterministic route validation, and long headless UI stress runs
- 100,000 seeded full-run simulations across all characters and stakes
- No embedded interpreter or compatibility parser in the player

Keyboard and mouse are supported. Controller navigation is not included.
