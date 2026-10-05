# Design

## One-sentence pitch

A roguelike where the only verb is "flip a coin", and every interesting decision is made *before* the flip: which
coins you own, what order they come in, what you throw away, and when you stop.

## Pillars

1. **Coins are the cards.** Each coin is a tiny program: a Heads chance, a Heads effect, a Tails effect, and
   sometimes a hook that bends the rules. A deck starts with 5 slots and grows to at most 10 by buying slots (5 gold each). Nothing else in the game scores points.
2. **No player HP.** Failure is running out of coins, not dying to damage. Bad outcomes (penalty effects) raise the
   quota instead of hurting you, so every flip is a gamble on tempo, not on survival. This keeps losing readable:
   "I did not have enough coins left to get there."
3. **Finite stack, no reshuffle.** A level lasts exactly as long as the coins you hold. Every coin is played once.
   Because the stack only shrinks, the player can count: coins left, points needed, average points per coin.
4. **Information first, luck second.** You see the next three coins in the bank, can peek at the pile with a chip,
   and see exact odds on every coin. Randomness decides *outcomes*, never what you are allowed to know.
5. **Discarding is free, flipping costs.** The cheap decision is to throw a bad coin away (no cost, but you lose a
   flip). The expensive decision is to flip a strong coin that costs energy. That tension is the heart of play.
6. **Small, readable numbers.** A normal coin is worth about half a point per flip; a strong one is 5 to 8. Quotas
   are 3 to 30. A player can do the arithmetic in their head.
7. **Everything seeded.** One RNG, one seed per run. The same seed and the same actions replay identically; this is
   what makes tests and the balance simulator possible.

## The core loop

```
 pick character + coin set
          |
          v
  +---> LEVEL: opening hand (discard freely) -> bank of 3 -> deal -> discard or flip -> resolve -> next coin
  |        |  quota met: payout now, keep flipping for surplus gold or Open Shop
  |        |  stack empty, quota not met: pay gold to exchange coins back, or the run ends
  |        v
  |      SHOP: buy coins / chips / a relic, tune odds, remove a coin, reroll
  |        |
  +--------+   (4 levels; the last is the boss, "The House")
```

A run is 15 to 25 minutes in the target design. A level is short (5 to 10 flips) because the stack is short.

## What makes the decisions interesting

- **Order and the opening hand.** You see 5 coins, discard any you do not want for the level, and the rest become the
  bank. Discarding a good-but-expensive coin you cannot afford is correct; discarding a weak coin to reach a strong
  one is a real trade because it removes a flip.
- **Energy.** You start each level with 3 energy. Strong coins cost 1 or 2 energy to flip. Energy only comes back
  from coin effects (Copper, Spark, Flux Capacitor), so a deck of heavy hitters needs an energy engine.
- **Quota is relative to deck size.** The quota per level is a per-coin rate times the number of coins, so
  buying a coin raises the target. You cannot just "add more coins"; you have to add *better* coins. Quality matters
  more than quantity once the deck is full.
- **Surplus is optional.** After the quota is met you may stop. Every two surplus points pay one gold, so greed pays,
  but each extra flip risks nothing (there is no HP) except the coins you would rather keep for a better moment.
  In practice the choice is when to open the shop, and it is cheap because a cleared level cannot be lost.
- **The combo.** Same result in a row multiplies points (x1 to x3). Discarding a risky coin to protect a streak is the one place where "throw it away" is a deep choice, and Domino, Twin and the forcing chips turn into engines.
- **Exchange.** When the stack is empty and the quota is not met, gold can buy three of your already-played coins
  back at a rising price. Gold is therefore also a second life, and a shop purchase competes with it.
- **The boss inverts every fifth flip.** That punishes plans that rely on a fixed outcome and rewards high-odds coins
  and spare capacity.

## Why it looks the way it does

- The theme is a pixel arcade/casino: teal felt screens, a gold frame, chunky buttons with drop shadows, a
  pixel font, and big multicolour pixel titles. The **shop screen is the reference**; every other full-screen view
  copies its frame, spacing and colours. See [ui-and-art.md](ui-and-art.md).
- Coin art is the identity of each coin: a flat colour and one emblem (sword, bullseye, droplet, skull). The
  player should recognise a coin by silhouette and colour before reading it.
- Heads and Tails are written on the coin sides, and the flip animation shows the real side moving: the result is
  decided before the animation starts, the animation only reveals it.

## After the boss

Winning is not the end: Endless Mode keeps adding levels with a growing quota (+0.5 per coin each level) and the House's inversion rule, so a lucky engine (Doubler, True
Echo, Amplifier, Megaphone, a long combo) has somewhere to matter, and the run only ends when a level is lost.

## Things that were tried and removed

| Removed | Why |
|---|---|
| Player HP and damage | Made losing feel like punishment from outside the coin game; replaced by quota penalties. |
| Draw budget per level | Reshuffling made levels long and made coin count irrelevant; a finite stack is simpler and countable. |
| Coin picking after a win | Added a screen that duplicated the shop. The shop after every level is the only way to get coins. |
| Reshuffling the discard | Turned the game into "play the best coin forever". Kept only as a test/simulation flag. |
| Energy shop items (reroll/force) | Dead code kept for tests; nothing in the UI calls them. |
| Tokens as a coin unlock currency | Unlocking now happens by buying the coin in the shop. Tokens are still earned but unused. |
| Resolve button | The flip result applies after a short hold; the player only clicks Next Coin. |
| Right-hand "Current Coin" panel | It duplicated the stage (name, odds, outcome). The stage now shows the coin, its two effects and the result. |

## Open design questions

- What are tokens for (or delete them)?
- Should surplus gold have a visible cap or a decay so the "stop or continue" choice is a real choice?
- Should exchange be available with no gold (a cheaper fallback), so a run never ends without a decision?
- Trader is the strongest character with a built set; Seer the weakest. Starting decks were evened out but the
  built sets are not (see [economy-and-balance.md](economy-and-balance.md)).
