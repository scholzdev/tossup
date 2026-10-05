# Lua → Unity parity audit

This is the **historical baseline audit**, before the fixes. See [REMEDIATION.md](REMEDIATION.md) for current implementation status, verification and remaining platform checks.

Unity does **not** fully reproduce the latest Lua version. All **152 original asset files** are present and byte-identical, and the content catalogs are substantially ported. Gameplay interactions, resume behavior, UI details, German localization, and development/release tooling still have gaps.

Audited on **2026-10-05**: Lua `main@63a7c55` against Unity `unity-6@89df462`, **including the existing Unity working-tree changes**. Lua references below mean files at that `main` commit; they can be read with `git show 63a7c55:<path>`. This report and [evidence.json](evidence.json) are the audit deliverables; gameplay files were not changed.

**Evidence labels:** **Paired** = the same focused scenario executed against Lua and C#; **Headless** = executed against Unity's actual UI/actions with a test platform; **Source** = verified by comparing implementations or files. Visual/controller findings were inspected in source, without a Unity player or physical controller test. This is an inventory of established gaps, not a claim that every possible interaction has been proven equivalent.

## What is already present

The catalogs contain the same 57 coins, 11 chips, five relics, three characters, eight route levels, eight stakes, eight modifiers, five contracts, five run encounters, and eight augments. Compared coin IDs, base numeric definitions, printed effects, character decks/pools, route, and stake data match. Presence in the catalog does not establish equivalent execution or presentation.

The original asset inventory is complete:

| Asset directory | Files | Missing or changed |
| --- | ---: | ---: |
| coins | 58 | 0 |
| items | 11 | 0 |
| relics | 5 | 0 |
| characters | 3 | 0 |
| encounters | 5 | 0 |
| augments | 24 | 0 |
| ui | 31 | 0 |
| fonts | 1 | 0 |
| music | 1 | 0 |
| sfx | 13 | 0 |
| **Total** | **152** | **0** |

The manifest and SHA-256 hashes are in `evidence.json`. There are asset **usage and import** differences below, despite no missing original files. German locale text is a separate content comparison.

## Fix first: resume failures

### P1 — Continuing during a flip can permanently stall the run

**Headless; critical.** Start a normal run, finish setup, flip a coin, allow an update to autosave, then reload. Unity saves `Pending` but clears `FlipAnimation` and sets `ResolveTimer=0` during load. After ten seconds of updates, the pending flip remains unresolved and another flip is forbidden. The probe returned `pendingSaved=true`, `loaded=true`, `pending=true`, `animation=false`, `timer=0`, `canFlip=false`.

Lua only saves an untouched level setup, shop, or augment choice; continuing restarts that level. Unity considers every encounter with `Mulligan == null` safe, including mid-flip.

Owners: [RunSave.cs:22](../../Assets/Scripts/Core/RunSave.cs#L22), [Actions.cs:50](../../Assets/Scripts/UI/Actions.cs#L50), [Actions.cs:499](../../Assets/Scripts/UI/Actions.cs#L499). Lua: `src/ui/actions.lua:39`, `:444`, `src/game.lua` snapshot/restore.

### P2 — Shop coin removal makes a valid run fail to resume

**Paired; high.** Remove a coin in the shop, encode the run, then decode it. Lua accepts the snapshot; Unity returns null. Removal leaves that coin's UID in the completed encounter's queue/pile/played display data. Unity validates those obsolete references against the current deck even in the shop; Lua explicitly allows obsolete completed-level references there. `A.LoadRun` deletes a rejected save, so the run is lost.

Owners: [Game.cs:951](../../Assets/Scripts/Core/Game.cs#L951), [RunSave.cs:46](../../Assets/Scripts/Core/RunSave.cs#L46), [Actions.cs:54](../../Assets/Scripts/UI/Actions.cs#L54). Lua: `src/game.lua:1650` and shop removal.

## Gameplay discrepancies

These features exist in C#, but their Lua behavior is not fully ported.

| ID / evidence | Lua behavior | Unity discrepancy and reproduction | Responsible source |
| --- | --- | --- | --- |
| G1 / Paired — side bets | Settle once, on the first flip. | Bet Heads from 50g, pay 5g, win 8g: first flip leaves 53g in both; a second Heads leaves Lua at 53g and Unity at **61g**. Refunds, Hedge Fund loss penalties, and displayed settlement can also repeat. | `Core/Game.cs:681`; Lua `src/game.lua:1125` settlement guard. |
| G2 / Paired — last-coin emergency permission | A final remaining coin can be flipped with insufficient energy. | Play Normal, leave Hammer, set energy to zero: Lua allows the flip; Unity refuses because it counts all owned coins, including already played ones. | `Core/Game.cs` `CanFlip`; Lua `Game.can_flip`, `Game.coins_left`. |
| G3 / Paired — emergency fee | An emergency flip costs up to 2g for unpaid energy. | Single Hammer, zero energy, 50g: Lua deducts to **48g**, Unity remains **50g**. | `Core/Game.cs` `Flip`; Lua `src/game.lua:788`. |
| G4 / Paired — Echo metadata | Copy effect amount, duration, and coin type. | Whetstone → Echo loses `kind="steel", coins=2`; Megaphone → Echo loses its two-coin multiplier duration. C# copies only type/amount. True Echo uses a complete copy. | `Content/Content.cs:232`; Lua `content/coins/echo.lua:13`. |
| G5 / Paired — Broken Clock protection | Every tenth flip is final Heads, protected from the House inversion. | On an inverting stage with nine flips completed, the next flip is **Heads in Lua, Tails in Unity**. C# omits the outcome's `final` protection. | `Content/Content.cs:610`, `Core/Game.cs:375`, `Core/Signal.cs` event model; Lua `content/relics/clock.lua`, `src/game.lua:724`. |
| G6 / Paired — upgrades with Swap | A Heads-score upgrade follows the effect side chosen after Swap. | Lucky Day Normal + Swap + Tails scores **2 in Lua, 1 in Unity**. Conversely, upgraded Heads swapped to Tails can receive a score bonus incorrectly. | `Core/Game.cs:703`; Lua `src/game.lua:1189`. |
| G7 / Paired — Type Specialist with Chaos | Add the specialist effect before Chaos duplicates resolved effects. | Doppelganger → Contrarian with Chaos specialization yields score effects **[4,1,4,1]** in Lua, **[4,4,1]** in Unity. | `Core/Game.cs:710`, `Core/LatestSystems.cs:91`; Lua resolve/run-hook ordering. |
| G8 / Paired — Cash Out reset | Reset the combo even when its gold pot is zero. | Cash Out as the first Heads: Lua combo length becomes **0**; Unity leaves **1 / Heads** because `BankComboPot` returns early on zero gold. | `Core/Game.cs:753`, `Core/LatestSystems.cs:247`; Lua `src/game.lua:1278`. |
| G9 / Paired — Amazon Prime failure timing | Mark failure when quota clears; remove coins when leaving the level. | Clear quota with fewer than three unplayed coins in a three-coin deck: Lua retains **3** until shop entry, Unity immediately drops to **1**, removing optional remaining plays. | `Core/LatestSystems.cs:160`, `Core/Game.cs` `EndLevel`; Lua contract callbacks and `content/contracts/amazon_prime.lua`. |
| G10 / Paired — shop odds | Exclude encounter odds hooks and contract penalties from shop probabilities. | Normal + previous Quick Clear contract in shop: **65% Lua, 60% Unity**. Momentum/Hourglass hooks can also read the finished encounter. | `Core/Game.cs:132`; Lua `src/game.lua:245`. |
| G11 / Source — Upgrade encounter's starting reward | Random Common reward comes from the character's usable/unlocked pool. | C# draws from `ShopPool`, which includes locked coins. It can grant a Common the Lua reward cannot choose. | `Core/LatestSystems.cs:145`; Lua `content/encounters/upgrade.lua`, `src/game.lua:180`. |
| G12 / Source — seeded upgraded shop offers | Sort each coin's upgrade keys before building the RNG selection list. | C# uses dictionary insertion order. Copper (`copper_lining`, `bright_side`) and Loaded (`safer_bet`, `gilded_face`) have the opposite order, so the same RNG index can select a different upgrade. | `Core/LatestSystems.cs:255`; Lua `src/game.lua:204`. |
| G13 / Paired — returning a coin twice | A returned UID can appear only once among live coins. | Mimic copies Lucky under an active Chaos buff: Lua returns it once, live UIDs **[2,1]**; Unity returns it twice, live UIDs **[2,1,1]**. C# inserts the UID again after it has already left `Played`. | `Core/Game.cs:595`; Lua `src/game.lua:949`, `:1039`. |

All Unity paths in these tables are relative to `Assets/Scripts/` unless explicitly prefixed otherwise. The paired fixtures use seed 6 and controlled outcomes to isolate the rule; they do not depend on randomly landing Heads. G13 injects the active Chaos buff directly, representing the buff supplied by Doppelganger.

## Other persistence gaps

| ID / evidence | Gap | Sources |
| --- | --- | --- |
| P3 / Source | **No Lua save migration.** Unity reads `profile.json` / `run.json` in its own persistent-data directory. Lua uses `profile.lua` / `run.lua` under the LÖVE identity `tossup`. Existing unlocks, collection, sets, options, tokens, and saved runs are not imported. | `UI/Actions.cs:9`, `TossupApp.cs:74`; Lua `src/profile.lua`, `src/ui/actions.lua`, `conf.lua`. |
| P4 / Paired + Source | **Profile validation is incomplete.** Malformed fixture: Lua clamps tokens −3→0, Blade stake 99→8, master volume 200→100, invalid language→en. Unity retains −3 / 99 / 200 / xx. It also accepts unknown IDs and oversized/invalid sets. An unchecked stake can index past `Game.Stakes` in character selection; unknown set IDs can fail during drawing/editing. | `Core/Profile.cs:284`, `:99`, character selection/set views; Lua `src/profile.lua:270`. |
| P5 / Paired | **Set trimming chooses different coins.** For unlocked `[normal, normal, normal, compost]`, Lua keeps `[normal, normal, compost]`; Unity keeps `[normal, normal, normal]`. Lua's rarity-limit repair preserves later picks; C# preserves earlier picks. | `Core/Profile.cs` `Loadout`; Lua `src/profile.lua:137`. |
| P6 / Source | **Run restore validation is missing several Lua checks.** Unknown/duplicate augment IDs, inconsistent pending augment choices, contract option/result validity, shop upgrade validity, and several ranges/maps are unchecked. `Enum.Parse` also permits numeric enum strings. Such data can pass decoding and fail later in views/actions. | `Core/RunSave.cs:46`, `StateJson.Decode`; Lua `src/game.lua:1570`–restore validation. |

## UI and controls not fully ported

| ID / evidence | Missing or incomplete behavior | Sources |
| --- | --- | --- |
| U1 / Headless + Source | **Normal run setup differs.** Latest Lua automatically finishes the opening mulligan after saving the untouched level, and disables contract offers in the normal app flow. Unity leaves the opening mulligan visible; Start Level enters Contract. The underlying Lua contract/mulligan systems exist, but this is not the latest Lua player flow. | `UI/Actions.cs:346`, `:439`, `:457`; Lua `src/ui/actions.lua:275`, `:461`. |
| U2 / Headless + Source | **Clicking the large central coin does not flip/advance.** Lua registers the 270×270 central action area; Unity has no action at logical coordinate `(770,385)`. The lower Flip button still works. | `UI/Views/EncounterView.cs:263`; Lua `src/ui/views/encounter.lua` central coin action. |
| U3 / Source | **Coin-bank hover metadata and name clipping are missing.** Lua registers bank coin hover details and scissors long names. Unity neither registers `CoinHover` for those rows nor clips the name; long names can overlap probability/energy text. | `UI/Views/EncounterView.cs:195`; Lua `src/ui/views/encounter.lua:152`. |
| U4 / Source | **Coin tooltips omit Edge, rarity, types, Fortune bonus, and upgrades.** Unity shows only Heads/Tails and computes Tails as `100 − Heads`, ignoring Tie. `HoveredCoin` cannot carry Tie or upgrade data. Upgraded shop offers are labeled UPGRADED but cannot explain which upgrade or its odds/effect change. | `UI/Draw.cs:251`, `UI/UiState.cs:20`, shop `CoinHover` calls; Lua `src/ui/draw.lua:232`. |
| U5 / Source | **Square Dance's explanatory Heads text is absent.** Lua carries `heads_description` explaining 1/2/3 copies → 2/8/18 points. C# lacks that field and renders the empty printed Heads list as no effect. The scoring hook itself exists. | `Core/Model.cs` coin definition, `UI/Draw.cs`, `UI/Views/EncounterView.cs:317`; Lua `content/coins/square_dance.lua`. |
| U6 / Source | **Latest tooltip layout rules are absent.** Lua wraps each effect/upgrade row, expands the panel, anchors set-editor details at the left outside the catalog, and suppresses the generic tooltip in encounters. Unity only expands the description, keeps fixed effect rows, follows the pointer over the catalog, and lacks the encounter suppression. | `UI/Draw.cs:251`; Lua `src/ui/draw.lua:232`. |
| U7 / Source | **Owned chips/prizes panel is missing in the shop.** Lua shows owned icons with effect hovers; Unity replaces them with a flat HELD text list. The asset files are present. | `UI/Views/ShopView.cs:151`; Lua `src/ui/views/shop.lua:58`. |
| U8 / Source | **Locked deck-slot padlocks and next-slot explanation are missing.** Buying the next slot exists, but Lua's lock drawing and “bigger deck means bigger quota” hover are absent. | `UI/Views/ShopView.cs:160`; Lua `src/ui/views/shop.lua` locked slots. |
| U9 / Source | **Controller inspection does not associate Buy buttons with their offer artwork.** Lua finds nearby hover regions above the focused button and anchors their tooltip. Unity substitutes only the focused button's coordinates as the mouse position; Buy button centers lie outside the artwork's hover rectangle, so coin/chip/prize details are unavailable there. | `UI/PadNavigation.cs:198`, `UI/UiState.cs:122`, `UI/Views/ShopView.cs`; Lua `src/ui/pad.lua:175`. |
| U10 / Source | **Controller shortcut badges are missing.** Lua draws connected-controller A/B/X/START badges from button hotkeys. C# buttons have no hotkey field and focus drawing has no badge equivalent. | `UI/UiState.cs:7`, `UI/PadNavigation.cs` `DrawFocus`; Lua `src/ui/pad.lua:194`, `src/ui/draw.lua:36`. |
| U11 / Headless | **Keyboard chip numbers are off by one.** Mouse uses indices 0–2; keyboard passes 1–3. With Energy Drink, Shortcut, Safety Net, key 1 consumes Shortcut instead of Energy Drink. Key 2 uses chip 3; key 3 cannot use the third chip. | `UI/AppCore.cs:218`, `UI/Actions.cs:424`; Lua keyboard slot handling. |
| U12 / Source | **Edge flip animation is incomplete.** Lua uses 9.5 half-turns, finishes edge-on at squash .06, hides face text near the edge, and draws a separate purple EDGE banner. Unity uses ten half-turns for Tie, finishes face-on, and draws EDGE inside the rotating face with Heads/Tails color. | `UI/Views/EncounterView.cs:79`; Lua `src/ui/views/encounter.lua:28`. |
| U13 / Source | **Encounter reveal omits moving rays, expanding rings, icon rotation, and icon fade.** Unity retains the panel/rise/pulse but simplifies the rest. The original encounter art is present. | `UI/Views/LatestViews.cs:143`; Lua `src/ui/views/encounter_reveal.lua`. |

**Additional mismatch:** Unity still offers the purchasable **Odds Tuner**, which latest Lua removed from the shop. This is extra legacy behavior, rather than a missing feature. Full parity requires aligning the shop with the chosen Lua reference. Sources: `UI/Views/ShopView.cs:124`, `Core/Game.cs:968`; Lua `src/ui/views/shop.lua` deck-tool panel.

## Localization and content text

### L1 — German translation coverage is incomplete

**File comparison.** Lua has 463 flat German source-string keys. Unity has 224, including legacy-only keys; **267 Lua keys are missing**. Therefore the missing count is not simply 463−224.

- **14 coin entries missing:** Blood Pact, Compost, Conductor, Counterfeiter, Crystal Ball, Doppelganger, Good Dog, Horoscope, Jackpot, Lifeline, Mimic, Orchestra, Square Dance, Whetstone.
- **Four chip entries missing:** Energy Drink, Lucky Charm, Safety Net, Shortcut.
- **All eight modifier entries missing:** Blackout, Bonus Exchange, Cold Snap, Gold Rush, Good Rhythm, High Stakes, Lucky Day, Power Surge.
- Missing flat strings cover current tutorial, controls, Edge, combo/bet prompts, contracts, augments, run encounters, stakes, upgrade names/descriptions, and other current UI.

`evidence.json` contains **every missing key/ID**, plus changed flat translations. Owner: [de.json](../../Assets/Resources/locales/de.json); Lua `locales/de.lua`. Modifier translations also need their group lookup wired into C#: `UI/Lang.cs` has grouped helpers for coins/items/relics/characters, but none for modifiers.

### L2 — Seventeen coin descriptions differ, including incorrect rules

**Catalog comparison.** The complete 17-entry text diff is in `evidence.json`. These differences materially misdescribe gameplay:

| Coin | Current Lua description | Unity description still says |
| --- | --- | --- |
| Focus | Next coin gets +35% Heads. | Builds its own Heads chance. |
| Orchestra | Scores per different **coin type**. | Scores per different **coin**. |
| Snowball | Maximum growth +8. | Maximum +10. |
| Pot | Maximum 8 points. | Maximum 12. |
| Loaded | Heads gain, Tails loss, and Edge loss/gain. | Reliable income on Heads. |
| Cash Out | Squares multiplier, banks pot, resets combo. | Omits banking the pot. |
| Conductor | Next three Rhythm coins: 2 points/step, max 8; broken combo adds 5 quota. | Only says “gain points from the combo.” |
| Blood Pact | Edge receives half the Heads and Tails effects. | Omits Edge. |
| Doppelganger | Duplicates resolved effects **including penalties**. | Omits penalties. |
| Doubler | Explicit maximum of 384. | Omits the cap. |

Several existing German descriptions are similarly stale, including Focus, Loaded, Snowball, Pot, Cash Out, and the Flux Capacitor name. Updating descriptions is separate from changing the already-ported numeric rules. Owner: `Content/Content.cs` and `Assets/Resources/locales/de.json`; Lua `content/coins/*`, `locales/de.lua`.

### L3 — Some text bypasses translation even if the table is completed

**Source.** Encounter reveal uppercases the English name before lookup, while `Lang.T` requires an exact source-string match; the description is sent directly to `Gfx.Printf`. Pending augment details similarly print unlocalized concatenated text. These need call-site changes in addition to the missing German table entries. Sources: `UI/Views/LatestViews.cs:134`, `:157`; Lua encounter reveal/augment views.

## Sandbox parity

| ID / evidence | Gap | Sources |
| --- | --- | --- |
| S1 / Paired | **Override odds discard modifiers.** Blood override Heads .10 / Tie .80 plus +.07 all-odds: Lua probability **.17**, Unity **.10**. Unity returns the override before buffs, upgrades, Fortune, hooks, and clamping. | `Core/Game.cs:132`; Lua `src/game.lua:245`. |
| S2 / Paired | **Restricted shop pools are ignored.** Blood-only sandbox: Lua offers only Blood; Unity seed-6 stock offers Jackpot, Normal, Fuse, Square Dance from the character pool. | `Core/Game.cs:89`, `:101`; Lua `src/game.lua:157`. |
| S3 / Source | **Quota calculation ignores configured odds and initial energy.** C# `DeckQuota` uses base odds rather than sandbox overrides; custom energy is assigned after the initial quota is calculated, affecting energy-dependent estimates such as Flux Capacitor. Lua applies resources before starting the encounter and incorporates override odds into quota. | `Core/LatestSystems.cs:123`, `Core/Game.cs:343`; Lua `src/game.lua:334`, `:670`. |
| S4 / Source | **Default scene/fixtures are not fully migrated.** Lua ships `sandbox.lua` plus Blood/shop scenes and loads its default scene when sandbox mode is enabled. Unity ships only `tools/sandbox/example.json`; `TOSSUP_SANDBOX=1` without a scene path enables sandbox mode but does not load a default scene. JSON is a suitable replacement format, but the original scenarios/start behavior remain absent. | `TossupApp.cs:65`, `:100`, `tools/sandbox/`; Lua `sandbox.lua`, `scenes/`, app sandbox loader. |

## Asset presentation and platform behavior

| ID / evidence | Gap | Sources |
| --- | --- | --- |
| A1 / Source | **Encounter/augment pixel art import settings differ.** Lua uses nearest filtering, without the smooth mipmapped setup used for coins. The Unity postprocessor applies trilinear filtering and mipmaps to encounters/augments. All pixels are copied, but future imports/regeneration render these assets differently. Individual existing `.meta` overrides may differ from the postprocessor. | `Assets/Editor/TossupAssetImport.cs:34`; Lua `src/ui/app.lua:68`. |
| A2 / Source | **Window resizing is disabled.** Lua starts at 1280×800 and permits resizing down to 640×400. The current Unity working tree sets 1600×1000 and `resizableWindow=false`. This includes pre-existing local changes and is reported without changing that preference. | `Assets/Editor/TossupBuild.cs:70`, `ProjectSettings/ProjectSettings.asset:104`; Lua `conf.lua`. |
| A3 / Source | **Sound event parity is incomplete.** Edge should play `land_tails` at pitch 1.35; Unity uses 1.0. Lua resets sound-watch baselines when the game instance changes; Unity does not, so loading a shop or a prior resolved result can synthesize transition/result sounds. | `UI/Sound.cs:36`; Lua `src/sound.lua`. |
| A4 / Source | **Version/build diagnostics are missing.** Unity title hardcodes `v0.1.0 … (dev)`; run logs omit Lua's version/build and stake fields; there is no equivalent timestamp/version/trace `crash.log` hook. Unity's ordinary Player.log is a different diagnostic artifact. | `UI/Views/MenuViews.cs:55`, `UI/Actions.cs:118`, `TossupApp.cs`; Lua `src/version.lua`, `src/build_id.lua` packaging, `src/ui/actions.lua:97`, `main.lua:5`. |

## Development, documentation, and release tooling

These are outside in-game behavior but belong to a complete project port.

| ID / evidence | Gap | Sources |
| --- | --- | --- |
| T1 / Source | **No Unity Linux build/package path.** Current builder accepts macOS/Windows only. Lua includes Linux AppImage packaging. | `tools/build_unity.sh`, `Assets/Editor/TossupBuild.cs`; Lua `tools/build_linux.sh`. |
| T2 / Source | **Balance simulator has no equivalent.** Lua's `tools/sim.lua` supports bots, per-coin reports, clear-rate/trace outputs, and stake/quota/payout overrides. Unity's UI smoke test and older deterministic parity bot do not replace this workflow. | Unity `tools/`; Lua `tools/sim.lua`. |
| T3 / Source | **All-platform release workflow is absent.** Lua has test gates, version/build stamping, packaged artifacts, release notes, tagging, and GitHub release publishing. Unity has a two-platform build script but no corresponding `build_all`/release pipeline. | `tools/build_unity.sh`; Lua `tools/build_all.sh`, `tools/release.sh`, build scripts. |
| T4 / Source | **Docs/wiki regeneration is broken.** Both current shell wrappers invoke deleted `tools/dump_content.lua`. The wiki generator also reads deleted `src/version.lua`. Retained Lua documentation tests are not wired to a Unity content exporter. | `tools/build_docs.sh:7`, `tools/build_wiki.sh:6`, `tools/gen_wiki.py:539`, `tests/test_docs.sh`. |
| T5 / File comparison | **Automatic wiki publishing workflow was removed.** The Lua branch has `.github/workflows/wiki.yml`; Unity has no replacement workflow. | Lua `.github/workflows/wiki.yml`; current branch file inventory. |
| T6 / Source | **Existing parity bot does not cover latest Lua systems.** It uses 1–10 coin loadouts despite the new five-coin set limit, lists only the original seven chips, maps Contract/Augment to GAME_OVER, and does not trace current upgrade/augment/contract/Tie/combo-pot systems comprehensively. Historical parity claims cannot establish latest-main parity. | `tools/parity/csharp/Program.cs:16`, `:35`, `:142`. |

## Verification and boundaries

Executed the existing focused core/UI tests, **1,000-seed sweep**, **23-screen tour**, **1,000 monkey frames**, and **1,000 player frames**. They passed. The additional targeted probes above still demonstrate parity failures; passing those smoke checks is insufficient evidence of a complete port.

Existing test invocation used `DOTNET_ROLL_FORWARD=Major` because this machine lacks the .NET 8 runtime while newer runtimes are installed:

```sh
DOTNET_ROLL_FORWARD=Major dotnet tools/uitest/bin/Release/net8.0/UiTest.dll 1000 11
```

Focused audit probes ran the real current C# Core/Content/UI code and the extracted Lua commit. Lua 5.5 required a single-result `require` wrapper to match the LuaJIT loader behavior. The separate headless platform used a temporary save directory. Raw paired results, original asset hashes, all missing German keys, and all 17 description differences are preserved in [evidence.json](evidence.json).

No native Unity player build, visual screenshot comparison, physical controller check, or Windows/Linux runtime test was performed in this audit. G11/G12 and the explicitly source-labeled findings remain source-established rather than paired end-to-end reproductions.

Two possible false positives were excluded: the documented D/Y discard shortcuts are absent from the latest Lua runtime too, and the subsequent-successful-push All-In penalty behavior is shared by both implementations. Neither is reported as an unported Lua feature.

The port should only be called complete after these established gaps are resolved and the current Lua reference has dedicated parity coverage for saving/removal, repeated bets, emergency flips, typed/duration copying, buff/upgrade interactions, current UI/controller behavior, localization, and supported packaging workflows.
