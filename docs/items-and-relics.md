# Chips and prizes

The game has two kinds of non-coin objects. **Chips** are consumables you use during a level. **Prizes** (relics) are
passive effects that last the whole run. Both are bought in the shop.

## Chips (items)

- Sold in the shop, 2 offers per visit from the 11 chips. You can hold **3** at most (`Items.MAX`).
- Used with the three slots at the bottom right of the round view. A chip is only usable while a coin is dealt and not being flipped.
- Using a chip consumes it, unless it refuses (then nothing happens and it stays).
- One-shot effects are *armed* on the event bus and fire once on the next matching event; any that are unused when the level ends
  are cleared.

<!-- GEN:chips -->
| Chip | Cost | Effect | Notes |
|---|---|---|---|
| **Double Down** | 14 | Next coin: points, gold, energy and penalties x2 | Armed on `coin_resolve`; doubles score, gold, energy and penalties. Best on Hammer, Blood, Gambler or Cursed. Beware: it doubles a penalty. |
| **Energy Drink** | 8 | Gain 2 energy | Unlocks a Hammer or Blood flip you could not pay for. |
| **Extra Draw** | 10 | A played coin returns to the pile | Maximum 3 returns per level (shared with Lucky and Hourglass). Refuses if no coin was played or at the cap. |
| **Force Heads** | 12 | Dealt coin lands Heads | Armed on `coin_flip`. A relic or the boss can still change the side afterwards. |
| **Force Tails** | 12 | Dealt coin lands Tails | For Phoenix (store anger), Martyr (debt) and Contrarian planning. |
| **Lucky Charm** | 10 | The next 2 coins +20% Heads | Shown immediately in the odds. |
| **Peek** | 6 | See the next two coins | The result is shown in the hint text. Refuses if the pile is empty. |
| **Safety Net** | 9 | The next combo break is prevented | Use it in the middle of a long streak before a risky coin. |
| **Shortcut** | 12 | Score 3 points at once | Counts toward the quota and the surplus gold like any points. |
| **Swap** | 8 | Free discard, new coin | Refuses if it would leave no usable coin. |
| **Weighted** | 8 | +25% Heads, one flip | Capped at 100%. |
<!-- /GEN:chips -->

**Using chips well**

- Chips are cheaper than coins (6 to 14), but are one-shot. The best use is on the highest-value flip of the level.
- Force Heads plus Cursed (+15) is a guaranteed 15 points for 12 gold; Double Down plus Hammer is 32.
- Peek and Swap give information and control for little gold; they are strongest in decks with a few strong coins and many weak ones.

## Prizes (relics)

- 1 offer per shop visit, 25 gold. You never see one you already own.
- No limit on the number owned. They bind to the event bus when bought (`src/relics.lua`).

<!-- GEN:prizes -->
| Prize | Effect | Notes |
|---|---|---|
| **Baton** | Combo: +0.4 per step instead of 0.25, up to x4 | Built for streak decks (Hot Hand, Anchor, Domino, Twin). |
| **Broken Clock** | Every 10th flip is Heads | Only matters in long levels (a 10-coin stack with returns); weak in short ones. |
| **Magnet** | Every 3 Heads in a row: +5% Heads this level | Streak-based. With Momentum or Chain this snowballs. Level bonus resets each level. |
| **Metronome** | Every 4th flip pays double | Pairs with Megaphone: put the Megaphone buff on the 4th flip. |
| **Lucky Penny** | First Tails each level becomes Heads | Applied after the roll and chips. Makes the first flip a safe one; best on a coin with a big Heads. |
<!-- /GEN:prizes -->

**Relic order.** Relics change the outcome in `coin_outcome`, after chips and coin hooks and before the boss inversion. The
boss still inverts every fifth flip afterwards, so a Lucky Penny flip on the boss's fifth flip becomes Tails.

## Ideas not yet implemented

- A relic that raises max energy (the shop function `buy_energy` exists but is not wired to anything).
- A relic that makes discards give gold.
- Chips that change a coin's side effects rather than odds (for example "swap Heads and Tails effects for this flip").
