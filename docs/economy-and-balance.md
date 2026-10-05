# Economy and balance

All numbers are constants in `src/game.lua` (the level table `Game.route`, `START_GOLD`, `SURPLUS_RATE`, `EXCHANGE_*`) or
in the content files. The headless simulator (`tools/sim.lua`) plays whole runs with three bots and is the source of
every rate below. The bots are crude, so use the numbers to find broken coins and a broken economy, not to judge fun.

## Resources

| Resource | Source | Sink |
|---|---|---|
| **Points** | Coin effects (`score`) | Meeting the quota. Surplus converts to gold. |
| **Gold** | Start 25, level payouts (25/30/35), surplus (1 per 2 points over quota), gold coins (Copper, Loaded, Bank, Vampire, Bounty) | Shop, exchange |
| **Energy** | 3 per level, Copper/Spark/Flux Capacitor | Flipping coins with an energy cost |
| **Coins in the stack** | The deck, exchange returns, extra draws | One per flip |
| **Tokens** | 1 per level cleared, +3 for a won run | Nothing (unused) |

Gold is the only resource that persists between levels, so it is the long-term resource.

## Gold flow in a typical run

- Start with 25. Payouts are 25 after level 1, 30 after level 2 and 35 after level 3, plus surplus, minus whatever you spend.
- A typical shop visit after level 1 buys: 1 coin (15), 1 odds tuner (10) or a chip, with a reroll or two left over.
- Prices: coin 15 (Normal 5), chip 6-14, relic 25, tuner 10, removal 8, reroll 4 (+2 each).
- An exchange costs 10, 15, 20... within one level. At the start of a run the 25 gold can pay for at most two exchanges (10 + 15).

## Deck slots

The deck starts with 5 slots (and a coin set holds 5 coins). The shop sells up to 5 more at 5 gold each (25 gold for all of them), so growing the deck is a purchase like any other and
competes with coins (15), chips and exchanges. Since the quota scales with deck size, a slot also raises the next quota by the per-coin rate (0.6 to 3.0): buying a slot without a
good coin to put in it makes the level harder. With the new rule the best 5-coin sets clear the boss less often than the old best 10-coin sets (52 / 30 / 64% against 58 / 47 / 75%): the
power now comes from the shop, not from the coin set. The simulator's Seer set (best 5 coins by single-flip value) is the weakest because it picks high-variance coins.

## Quotas

`quota = round(per_coin x deck size)`: 0.6 / 0.9 / 1.6 / 3.0. (The last two were raised from 1.5 / 2.6 when the combo multiplier arrived; it lifted built sets' boss clear rate by about 15 points.)

| Deck size | L1 | L2 | L3 | Boss |
|---|---|---|---|---|
| 5 | 3 | 5 | 8 | 15 |
| 7 | 4 | 6 | 11 | 21 |
| 10 | 6 | 9 | 16 | 30 |

Required average points per flip, with a full stack (the number of flips equals the number of coins, ignoring exchange):
0.6 / 0.9 / 1.6 / 3.0 points per coin. A Normal coin averages 0.5; Sword 2.5; Dagger 3.25; Hammer 6.25; Blood 4.2 net. So a deck
of mostly Normal coins clears level 1 about half the time and fails levels 3 and 4; a deck averaging 3 points per coin clears
everything but the boss only some of the time.

## Simulator results (smart bot)

Run with `lua tools/sim.lua --runs 300` (default: a built set of the best 5 coins; the bots buy deck slots in the shop) and `--set default` (the plain 5-coin start).

| Setup | Level 1 | Level 2 | Level 3 | Boss |
|---|---|---|---|---|
| Built 5-coin sets (Blade / Seer / Trader) | 77-100% | 61-100% | 48-96% | 52 / 30 / 64% |
| Plain default deck | 87-100% | 50-75% | 25-48% | 5-15% |

Notes:

- **The plain start is hard but fair:** the quota is 3 points on 5 coins. The exchange at 10 gold lifts level 1 from about 51% to
  about 85% for the plain deck: the starting 25 gold buys at least one exchange.
- **A built set is much stronger than a plain one.** The quota scales with deck size, not with coin quality, so a deck of strong
  coins has a better ratio. This is intended (it makes coin sets and unlocking matter), but the gap is large.
- **Trader is the strongest** because gold doubles as extra lives; **Seer is the weakest** with a built set.

## Surplus gold

After the quota is met, each 2 points over pays 1 gold, rounded down on the running total. A strong deck may therefore
earn 5-10 extra gold in a level by continuing to flip. This is the main reason a level stays open after the quota is met: choosing when to leave is the
reward for playing well. There is no downside to flipping on except that the coins are spent.

## Known balance problems

1. **Prices (partly fixed).** Prices were flat at 15; the strongest and weakest coins now cost 22-24 and 10-12 (see coins.md), but the rest are still 15 and the ratios are untested with real players. Before the change Snowball, Hammer, Blood, Flux Capacitor and Gambler are under-priced compared to
   Hourglass, Lucky or Flock. Suggested: strong coins 20-25, weak 10-12.
2. **Gold coins are weak late.** Bank and Miser need gold, and gold rarely accumulates in a 4-level run.
3. **Seer is behind.** Its starter has no scorer. Candidates: give the Seer Dagger in its starting pool, or make Cursed 30%.
4. **Tokens do nothing.** Either spend them (new characters, extra starting gold) or delete them.
5. **Surplus rate is untuned.** 0.5 gold per point is a guess; with a strong deck it dominates the economy.
6. **Quota does not scale with coin quality.** A built set trivialises levels 1-2. Could scale per_coin with the average deck power, or
   add a modifier per level (boss rules, a level "curse").
7. **Dead code.** `Game.reroll`, `Game.force` (energy-based) and `Game.buy_energy` are kept for tests only.

## How to rebalance

```
lua tools/sim.lua --runs 500                       # built sets, all bots, all characters
lua tools/sim.lua --set default --char blade        # plain start
lua tools/sim.lua --coins                           # per-coin strength table
lua tools/sim.lua --quota 0.6,1.0,1.6,2.4          # override the per-coin quota
lua tools/sim.lua --payout 20,25,30                 # override level payouts
lua tools/sim.lua --trace 381927 --char seer       # full log of one run
```

Change one thing at a time and run at least 300 runs; per-level rates move by 3 to 5% from sampling noise.
