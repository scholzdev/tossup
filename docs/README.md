# Tossup documentation

Tossup is a single-player coin-flipping roguelike ported from Lua / LÖVE to Unity. You build a small stack of coins,
flip them one at a time and score points against a quota before the stack runs out. These documents describe
what the game is, why it is built that way, and how it works.

| File | What it covers |
|---|---|
| [design.md](design.md) | Vision, design pillars, the core loop, what makes decisions interesting, what was cut and why |
| [gameplay.md](gameplay.md) | Every rule in order: run, level, opening hand, bank, flip, resolve, quota, exchange, shop |
| [coins.md](coins.md) | Coin numbers, expected value, hooks, uses and pairings |
| [characters.md](characters.md) | The three characters, their starting decks, pools, locked coins and play styles |
| [items-and-relics.md](items-and-relics.md) | Chips (consumables) and prizes (relics): effects, timing, tips |
| [economy-and-balance.md](economy-and-balance.md) | Gold, quotas, payouts, shop prices, simulator results, known balance problems |
| [ui-and-art.md](ui-and-art.md) | Screens, layout grid, palette, fonts, generated art, cursor, how to keep the look consistent |
| [architecture.md](architecture.md) | Original Lua architecture and the current Unity build chain |

The older single-file reference [`../GAME_SPEC.md`](../GAME_SPEC.md) is kept as a compact dump of rules, content tables
and code notes for pasting into prompts. If a document disagrees with the code, the code wins; then fix the document.
The [project README](../README.md) has current run and build instructions. [`../impl.md`](../impl.md) contains original design notes and is partly out of date.

**Deutsch:** Die Dokumente `gameplay`, `coins`, `characters`, `items-and-relics`, `design` und die Kurzanleitung (`play`) gibt es auch auf Deutsch in [de/](de/). Das lokale Wiki (`tools/build_wiki.sh`) zeigt sie auf der deutschen Seite.
