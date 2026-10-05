# Tossup documentation

Tossup is a single-player coin-flipping roguelike (Lua / LÖVE 11, LuaJIT). You build a small stack of coins,
flip them one at a time and score points against a quota before the stack runs out. These documents describe
what the game is, why it is built that way, and how it works.

| File | What it covers |
|---|---|
| [design.md](design.md) | Vision, design pillars, the core loop, what makes decisions interesting, what was cut and why |
| [gameplay.md](gameplay.md) | Every rule in order: run, level, opening hand, bank, flip, resolve, quota, exchange, shop |
| [coins.md](coins.md) | All 48 coins: numbers, expected value, how the hook works, how to use it, what to pair it with |
| [characters.md](characters.md) | The three characters, their starting decks, pools, locked coins and play styles |
| [items-and-relics.md](items-and-relics.md) | Chips (consumables) and prizes (relics): effects, timing, tips |
| [economy-and-balance.md](economy-and-balance.md) | Gold, quotas, payouts, shop prices, simulator results, known balance problems |
| [ui-and-art.md](ui-and-art.md) | Screens, layout grid, palette, fonts, generated art, cursor, how to keep the look consistent |
| [architecture.md](architecture.md) | Code layers, files, hook API, event bus, testing, simulator, how to add content |

The older single-file reference [`../GAME_SPEC.md`](../GAME_SPEC.md) is kept as a compact dump of rules, content tables
and code notes for pasting into prompts. If a document disagrees with the code, the code wins; then fix the document.
[`../PLAY.md`](../PLAY.md) is the short player-facing summary. [`../readme.md`](../readme.md) and [`../impl.md`](../impl.md) are the original
design notes the project grew from and are partly out of date.

**Deutsch:** Die Dokumente `gameplay`, `coins`, `characters`, `items-and-relics`, `design` und die Kurzanleitung (`play`) gibt es auch auf Deutsch in [de/](de/). Das lokale Wiki (`tools/build_wiki.sh`) zeigt sie auf der deutschen Seite.
