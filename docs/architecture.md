# Architecture

Lua 5.1 / LuaJIT on LÖVE 11.5. The rules are written without any LÖVE dependency so they can be tested and simulated with plain `lua`.

## Layers

```
content/   data: coins, items, relics, characters           (pure tables + hooks)
src/       rules: game, rng, signal, hooks, relics, items, profile   (no LÖVE)
src/ui/    state, actions (flow + clicks), draw helpers, theme, views (LÖVE drawing)
main.lua   forwards LÖVE callbacks to src/ui/app.lua
tests/ tools/ cli.lua   headless tests, simulator, one-run replay
```

Dependency direction: views -> actions/draw -> game/profile -> content. Rules never call UI code. Views draw only; they call
helpers in `src/ui/actions.lua` for everything that changes state.

## Files

| Path | Role |
|---|---|
| `src/game.lua` | All rules: `Game.new`, level start, bank, flip, resolve, discard, exchange, shop, probability, constants |
| `src/rng.lua` | Seeded minstd generator. Always use `RNG.random(game)` / `RNG.int(game, a, b)`; never `math.random` |
| `src/signal.lua` | Tiny event bus: `Signal.on/off/emit`. No `table.unpack` (LuaJIT) |
| `src/hooks.lua` | Routes a coin's hooks onto the bus while that coin is in play; documents the hook API |
| `src/relics.lua` | Binds owned relics to the bus |
| `src/items.lua` | `Items.use`, `Items.arm` (one-shot listeners), `Items.MAX = 3` |
| `src/lang.lua`, `locales/de.lua` | Text lookup: `Lang.t("BACK")` maps English source text to German through a plain table (unknown text is returned unchanged; extra arguments are `string.format`ted after the lookup, so `Lang.t("LEVEL %d / 4", n)` works). The table also has `coins`, `items`, `relics`, `characters` with `name` / `description` / `short`; `Lang.wrap` gives the UI catalogs that look those up. `src/ui/draw.lua` `text`/`centered` translate every string they draw. Language is the profile option `language` (Options screen). Adding a language: a new `locales/<code>.lua`, one line in `src/lang.lua`, title images in `tools/gen_ui_icons.py` |
| `src/profile.lua` | Saved meta progression: unlocks, collection, coin sets, options; pure serialization |
| `content/coins.lua`, `content/coin_order.lua`, `content/coins/*.lua` | Coin registry and one file per coin |
| `content/items.lua`, `content/items/*.lua` | Chips |
| `content/relics.lua`, `content/relics/*.lua` | Prizes |
| `content/characters.lua` | Starting deck, pool and locked list for each character |
| `src/ui/app.lua` | LÖVE load/update/draw/input, screen dispatch, images, cursors |
| `src/ui/state.lua` | Shared UI state: screen, game, marks, hover, draft set, animation, cursors |
| `src/ui/actions.lua` | Player actions and the per-frame flow: flip animation, hold, resolve, profile saves |
| `src/ui/draw.lua`, `src/ui/theme.lua` | Drawing primitives (box, outline, button, icon button, frame, tooltips), palette |
| `src/ui/views/*.lua` | One file per screen: title, menu (play screen), sets, collection, options, encounter, shop, finish |
| `tests/test_*.lua` | game, hooks, items, bank, profile |
| `tools/sim.lua` | Headless balance simulator (bots: random, greedy, smart) |
| `tools/gen_coin_icons.py`, `tools/gen_ui_icons.py` | Art generators (Pillow) |
| `runs.log` (save folder) | One line per finished run: time, seed, character, result, levels cleared, gold, reason, deck. Written by `log_run` in `src/ui/actions.lua` |
| `tools/build_all.sh` | Builds every platform into `dist/<platform>/` (love, macos, windows, linux); one failing platform does not stop the others. Each platform also has its own script: `build_love.sh` (the plain `.love`, which the others reuse), `build_macos.sh`, `build_windows.sh`, `build_linux.sh` |
| `tools/build_linux.sh` | Builds `dist/linux/Tossup.AppImage`: the official LÖVE 11.5 AppImage with the game appended (untested on a real Linux machine) |
| `tools/build_windows.sh` | Builds `dist/windows/Tossup-windows.zip`: `Tossup.exe` (LÖVE's love.exe with the game appended) plus the LÖVE DLLs; downloads the official LÖVE 11.5 Windows build once. Runs from macOS. The exe gets our icon (`assets/ui/icon.ico`) and the name "Tossup" through `tools/set_exe_icon.mjs` (pure JS, `resedit` from npm, so no Windows tools are needed); needs node |
| `main.lua` | Forwards LÖVE callbacks to `src/ui/app.lua` and wraps `love.errorhandler` so every crash is appended to `crash.log` in the save folder (with `runs.log`) |
| `src/serialize.lua`, `src/version.lua` | Plain-data serializer (tables to Lua source and back) used for the saved run; the game version number and the commit stamped by `tools/build_love.sh` into `src/build_id.lua` (git-ignored). `Game.snapshot` / `Game.restore` turn a run into data and back; only safe points (opening hand, shop) restore. Autosave is in `src/ui/actions.lua` (`A.load_run`, the key check in `A.update`) |
| `src/ui/tutorial.lua` | The interactive tutorial: spotlight steps over a scripted throwaway run (see ui-and-art.md) |
| `src/ui/sound.lua`, `tools/gen_sounds.py`, `assets/sfx/`, `assets/music/` | Sound effects and the music loop: generated WAVs, volume sliders, and a state watcher that plays the effects (see ui-and-art.md) |
| `tools/build_wiki.sh`, `tools/dump_content.lua`, `tools/gen_wiki.py` | Builds the local wiki into `wiki/` (git-ignored; open `wiki/index.html`): `dump_content.lua` writes the game's content (coins, chips, prizes, modifiers, characters, constants, German texts) as JSON, `gen_wiki.py` turns it plus `assets/` and `docs/` into static HTML: a filterable coin table, a page per coin / chip / prize / character, the modifiers, and the docs as guide pages; search is client-side. The wiki is built twice: English in `wiki/` and German in `wiki/de/` (names, descriptions and effect texts from `locales/de.lua`, page texts from the `DE` table in `gen_wiki.py`; the guides come from the German documents in `docs/de/` where they exist: `gameplay`, `coins`, `characters`, `items-and-relics`, `design` and `play` (the quick start); the developer guides stay English and are marked (EN). Keep `docs/de/` in step when the English ones change; the coin and chip notes on the wiki pages come from them), with an EN / DE switch in the header that jumps to the same page in the other language. Needs lua and Pillow. It follows the game data, so re-run it after any content change |
| `tools/run.sh` | `./tools/run.sh` starts the game with LÖVE; `./tools/run.sh app` checks the OS, builds the binary for it (macOS app, Linux AppImage, Windows exe under Git Bash) and launches it |
| `tools/build_macos.sh` | Builds `dist/macos/Tossup.app` (a renamed copy of LÖVE with the game and `icon.icns` inside, signed ad hoc) so the Dock and the app switcher say "Tossup" with the coin icon. `sh tools/build_macos.sh && open dist/macos/Tossup.app`. `dist/` is git-ignored; `love .` still shows LÖVE |
| `cli.lua` | `lua cli.lua <seed> <character>` plays and prints one automated run |

## Game state

`Game.new(seed, character, unlocked, loadout, manual_mulligan)` returns the `game` table:

| Field | Meaning |
|---|---|
| `phase` | `ENCOUNTER`, `SHOP`, `VICTORY`, `GAME_OVER` |
| `player` | `{gold, energy, max_energy}` |
| `coins` | The deck: `{uid, id, bonus, ...}` instances; hooks may add fields (`stack`, `charge`, `anger`, `debt`) that persist for the run |
| `encounter` | The current level: `combo_side`, `combo_len`, `shield`, `combo_step`, `combo_cap`, `buffs`, `quota`, `max_quota`, `scored`, `queue` (bank), `pile`, `discarded`, `played`, `returned`, `cleared`, `flips`, `streak`, `bonus`, `magnet`, `boss`, `exchanges` |
| `mulligan` | `{hand}` during the opening hand, otherwise nil |
| `dealt` | `{uid, probability}`: the coin waiting to be flipped |
| `pending` | The coin being resolved: `raw`, `result`, `altered`, `final`, `gained` |
| `last_result` | The previous flip (used by Echo, Contrarian) |
| `exchange_open` | The stack is empty and an exchange is possible (the UI shows the notice) |
| `items`, `relics`, `purchased`, `shop_offers`, `shop_items`, `shop_relic` | Inventory and shop |
| `lost_why` | Why the run was lost, shown on the run-over screen |
| `log` | Text log of everything |

## Coin hooks

A coin file returns `{name, description, rarity, probability, cost?, energy_cost?, heads = {effects}, tails = {effects}, <hooks>}`.
Effects are plain tables `{type = "score" | "gold" | "energy" | "penalty" | "extra_draw" | "probability", amount = n}`.

| Hook | When | May |
|---|---|---|
| `on_deal(game, inst)` | The coin becomes the dealt coin | react |
| `on_flip(game, inst, flip)` | After the raw roll | set `flip.result` |
| `on_resolve(game, inst, res)` | Before effects apply | edit `res.effects` (a private copy); `res.result` is the final side |
| `on_discard(game, inst)` | The coin is discarded | change instance state (Fuse) |
| `on_odds(game, inst, odds)` | Whenever odds are computed or displayed | mutate `odds.p`. **Must be pure.** |
| `grow(inst, event)` | `"level"` (every coin at level start), `"flip"`, `"discard"` | update persistent counters |
| `register(ctx)` | While the coin is in play | `ctx.on(event, fn)` to subscribe to the bus; torn down when it resolves |

Rules for hook code:

- Randomness only through `RNG`.
- `require("src.game")` lazily inside the hook, never at the top of a coin file (`src/game.lua` loads the coin files, so a top-level
  require is circular).
- No `table.unpack`; copy lists by hand.

### Bus events

`coin_deal`, `coin_flip{flip}`, `coin_outcome{flips, result}` (relics change `result`), `coin_resolve{res}`, `effect_applied{effect, text}`,
`coin_resolved{res}`, `coin_discard`, `encounter_start{encounter}`, `encounter_end{won}`. Scoped events carry `{game, inst}`.

## Chips and relics in code

- Chip: `{name, short, description, cost, use = function(game, Items) ... end}`. Return `false` to refuse (not consumed). Use
  `Items.arm(event, fn)` for effects that fire once on a later event.
- Relic: `{name, description, register = function(ctx) ctx.on(event, handler) end}`.

## Meta progression (`src/profile.lua`)

Saved as `profile.lua` in the LÖVE save folder (identity `tossup`). Fields: `unlocked[char][coin]`, `collected[coin]`,
`sets[char]` (3 sets), `active_set[char]`, `options`, and the unused `tokens`. Loading fills missing fields and migrates old
loadouts. `Profile.loadout(profile, char, max, max_copies)` builds the starting coins: the selected set, filtered to usable coins,
capped at 10 with the copy limit (Normal exempt); an empty set falls back to the character's default deck.

## Testing

```
lua tests/test_game.lua     # rules end to end
lua tests/test_hooks.lua    # every hook coin
lua tests/test_items.lua    # chips
lua tests/test_bank.lua     # bank, mulligan, discard, exchange
lua tests/test_profile.lua  # sets, unlocks, serialization
lua tests/test_combo.lua    # combo meter, combo coins, Baton
lua tests/test_endless.lua  # endless levels, Amplifier, True Echo, Doubler
lua tests/test_save.lua     # serializer, snapshot / restore of a run at its safe points
lua tests/test_lang.lua     # every coin, chip, prize and character has a German entry
```

All must print `... tests passed`. Tests compare logs of two runs with the same seed to guard determinism.

Scripts that run the real game (for screenshots) must set a different LÖVE identity than `tossup`, or they overwrite the real
save. A harness copies the project to a scratch folder, sets `t.identity = "tossup_test"` in `conf.lua`, and drives the UI with
a timed plan, calling `love.graphics.captureScreenshot`.

## Adding content

**A coin.** Add `content/coins/<id>.lua`; add the id to `content/coin_order.lua`; add a colour and an emblem to
`tools/gen_coin_icons.py` and run it (the game loads `assets/coins/<id>.png` for every coin and crashes without it); add the coin to a
character's `pool` or `locked` list; add a test in `tests/test_hooks.lua` if it has a hook; run `lua tools/sim.lua --coins`.

**A chip or relic.** Add the file; add the id to the order list in `content/items.lua` or `content/relics.lua`; add a colour and an
emblem to `tools/gen_ui_icons.py` and run it.

**An effect type.** Add a branch in `apply_effect` (`src/game.lua`) and the labels in `src/ui/draw.lua` (`effects`, `effect_description`).

**A screen.** Add `src/ui/views/<name>.lua` returning a draw function, register it in `src/ui/app.lua`, start it with
`D.frame(...)`, and add the navigation action in `src/ui/actions.lua`.

## Gotchas

- LÖVE runs LuaJIT, which has no `table.unpack` (use `unpack` or copy). Plain Lua 5.4 in tests does have it, so this only shows up in the game.
- All randomness through `RNG` for determinism.
- `box()` draws a drop shadow; the pixel font is `assets/fonts/m6x11plus.ttf`.
- Overlapping buttons: the last registered in `ui.buttons` wins.
- The fixed 1280x800 canvas means every position is in canvas coordinates.

## Generated documentation

`./tools/build_docs.sh` rewrites the data tables of the docs from the game's content: the coin overview and the coin headings (rarity, odds, energy, price) in `docs/coins.md` and `docs/de/coins.md`, the level and modifier tables in `gameplay.md`, the character tables, the chip and prize tables (their notes column is kept) and the content tables of `GAME_SPEC.md`. Generated parts sit between `<!-- GEN:name -->` markers; do not edit them by hand. `./tools/build_docs.sh --check` changes nothing and fails if a doc is out of date or a coin has no section; `tests/test_docs.sh` runs it. Prose is still written by hand.
