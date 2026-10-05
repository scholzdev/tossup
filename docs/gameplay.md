# Gameplay

This is the rulebook as implemented in `src/game.lua`. Constants live on the `Game` table.

## 1. A run

- **Four levels**, then the run is won. The last level is the boss, **The House**.
- You start with **25 gold**, **3 energy**, no chips and no relics, and a deck of up to 5 coins taken from the coin
  set you selected on the play screen. The deck has **5 slots** at the start; the shop sells up to 5 more (see 9).
- A run ends when you meet the boss quota (victory) or run out of coins in a level without meeting its quota and
  without taking an exchange (defeat).
- Between levels you visit the shop.

<!-- GEN:levels -->
| # | Level | Quota per coin | Quota with 5 / 10 coins | Payout |
|---|---|---|---|---|
| 1 | Opening | 0.7 | 4 / 7 | 25 gold |
| 2 | Second Chance | 1.4 | 7 / 14 | 30 gold |
| 3 | High Stakes | 2.5 | 13 / 25 | 35 gold |
| 4 | The House | 4.5 | 23 / 45 | none (ends the run) |
<!-- /GEN:levels -->

The quota is `round(per_coin x number of coins in your deck)` when the level starts, so buying coins raises the next
quota.

## 2. Anatomy of a level

1. **Start.** Energy refills to 3. The deck is shuffled into the draw pile. Coins that grow per level update.
2. **Opening hand.** Five coins are drawn from the pile and hover in the middle of a dimmed screen. Click coins to mark
   them, press Discard to throw the marked ones away for this level (free). At least one coin must stay. The first
   unmarked coin is the one that will play first. Press Start Level when happy.
3. **The bank.** The kept coins become the bank, an ordered queue. The left panel shows the next **three**. When the
   bank has fewer than three coins it refills from the draw pile. The pile is never reshuffled, so the whole stack only
   ever shrinks (apart from exchange and "extra draw" returns).
4. **Deal.** The first coin in the bank is the dealt coin. You may flip it or discard it, or discard other bank coins.
5. **Discard** is free. After the opening hand it removes only **the coin in play** (the one waiting to be flipped) for the rest of the level; the other bank coins cannot be picked. Not allowed during a flip or if no usable
   coin would remain. A discarded coin never comes back, even through an exchange.
6. **Flip.** If the coin has an energy cost you must have the energy. If you cannot pay, the coin cannot be flipped
   (discard it). The last usable coin always flips, paying what energy it can.
7. **Resolve.** The side's effects apply one after another, then the next coin is dealt.

## 3. The flip pipeline

Order of operations when you press Flip:

1. The energy cost is paid. The coin leaves the bank immediately and the bank refills.
2. A raw roll is made with the seeded RNG against the coin's current Heads probability.
3. Event `coin_flip`: the coin's own `on_flip` hook and armed chips (Force Heads/Tails, Weighted) may change the side.
4. `finalize`: event `coin_outcome` lets relics change the side (Lucky Penny, Broken Clock), then **the boss rule**
   (every 5th flip of The House inverts the side). The result is now fixed.
5. The animation spins the coin and lands on the fixed side. A banner shows HEADS or TAILS and, if something changed the
   roll, a line "ROLLED TAILS > HEADS (RELIC)".
6. After a short hold, **resolve**: copy the side's effects, event `coin_resolve` (hooks and Double Down may edit them),
   apply each effect, event `coin_resolved`, per-flip growth, quota check, deal the next coin.

## 4. Probability

`Heads chance = base + coin bonus + level bonus + hook modifiers`, clamped to 0..100%.

- **Base:** the coin's `probability`.
- **Coin bonus:** +10% for each Odds Tuner you bought for that coin, permanent for the run.
- **Level bonus:** Focus' effect and the Magnet relic add Heads chance for the rest of the level.
- **Hooks:** Momentum, Flock and Hourglass change the odds from the current state (`on_odds` is pure and recalculated for display).

## 4b. The combo

Every flip that lands on the same side as the one before raises the **combo**; a different side resets it to 1 (Heads and Tails both count).
The flip's points and gold are multiplied by `1 + 0.25 x (combo - 1)`, capped at x3 (nine in a row), then rounded:
x1, x1.25, x1.5, x1.75, x2 ... The multiplier is applied after hooks and buffs, so it stacks with Megaphone (a combo x2 on a doubled flip is x4).
Penalties are never multiplied. The combo shows in the top right of the stage (`COMBO HEADS x4`, `x1.75`) and the result banner
shows `+7 POINTS (x1.75)`. It resets at the start of every level.

It makes **discarding a tactical choice**: with a x2.5 combo running, throwing away a low-odds coin protects the streak. It also makes the
forcing tools strong (Domino, Twin, Force Heads, Weighted). Related coins: Hot Hand (extra step), Anchor (one free break), Bettor (points per step),
Cash Out (spends the combo), Cold Streak (Tails pay per Tails in a row). The Baton relic makes the step 0.4 and the cap x4.

## 4d. Level modifiers

From level 2 on every level (including the boss and every endless level) has one **modifier**, picked with the seeded RNG and shown in the stage's bottom left.

<!-- GEN:modifiers -->
| Modifier | Effect |
|---|---|
| Lucky Day | All coins +10% Heads. |
| Cold Snap | All coins -10% Heads, but the payout is 40% higher. |
| Power Surge | You start with 5 energy. |
| Blackout | You start with 1 energy, but the payout is 40% higher. |
| Gold Rush | Gold effects pay double. |
| High Stakes | Quota +25%, payout +50%. |
| Good Rhythm | The combo grows +0.4 per step. |
| Bonus Exchange | One extra exchange this level. |
<!-- /GEN:modifiers -->

## 4c. Endless mode

When the boss falls the run is won (VICTORY screen with the usual rewards). The screen offers **Endless Mode**: you go to the shop and keep playing levels
5, 6, 7... The run now only ends by losing a level. Each endless level asks **0.5 more points per coin** than the one before (3.5, 4.0, 4.5 ... per coin), pays
more (45, 50, ... gold) and **inverts every 5th flip** like The House. The header shows `LEVEL 5` and `ENDLESS 1`; the run-over screen counts the endless levels you cleared,
and `runs.log` gets a second line (`result=ENDLESS`) when it ends. This is where combo and buff engines are meant to go wild.

## 5. Effects

| Effect | What it does |
|---|---|
| `score` | Adds points toward the quota. Surplus beyond the quota pays gold. |
| `gold` | Adds gold. |
| `energy` | Adds energy (no maximum cap for gains; it refills to 3 each level). |
| `penalty` | Raises the quota (remaining and total) by the amount. Never reduces score. |
| `extra_draw` | Returns a played coin to the pile. At most 3 per level. |
| `probability` | +X Heads chance on that coin until the level ends. |
| `all_odds` | All coins get +X Heads chance for the rest of the level (Horoscope). |
| `peek` | Shows the next two coins of the pile (Peek chip). |
| `bank_discard` | Lets you discard one of the next three bank coins, free (Crystal Ball Tails). |
| `extra_exchange` | One more exchange this level (Lifeline). |
| `amplify` | Every active buff lasts one coin longer; Megaphone-style multipliers go up by 1, odds buffs double (max +60%). |
| `combo_bonus` | The combo grows by that many extra steps. |
| `combo_shield` | The next result that would break the combo is ignored (once per shield). |
| `next_mult`, `next_odds`, `next_swap`, `next_heads` | **Buffs** for the next N coins (x factor on points and gold, +X Heads, use the other side's effects, guaranteed Heads). They start with the coin after the one that made them and end with the level. |

## 6. Meeting the quota

When the remaining quota reaches 0 the level is **cleared** (once):

- The level payout is given immediately (25 / 30 / 35).
- The level stays open. Every **2 points beyond the quota pay 1 extra gold**.
- Keep flipping for more, or press **Open Shop** at the top right at any time when no flip is pending.
- If the stack runs dry after clearing, the shop opens by itself (unless an exchange is possible, then you choose).
- **The boss:** the run ends in victory the moment its quota is met.

## 7. Running out of coins

If the stack is empty and the quota is not met:

- **With enough gold:** a notice offers three buttons: **Buy More Coins** (exchange), **Start Again** (new run), and **Back
  To Menu**.
- **Without enough gold or with no played coin to return:** a **GAME OVER** notice appears over the round screen ("No coins left and not enough gold to exchange. The run is over."); OK (or Enter / Esc) continues to the run-over screen, which says why.

**Exchange:** pay gold to put up to **3** of the coins you already played this level (random, discarded coins excluded)
back into the stack. The price starts at **10** and rises by **5** for every exchange already made in the level. Repeat
while you can afford it, **at most 3 exchanges per level** (the popup shows how many are left). After the third, an empty stack with the quota unmet loses the run.

## 8. Energy

- 3 energy at the start of every level. There is no way to raise the maximum.
- Spent only to flip coins with an energy cost: Hammer 2; Blood, Snowball, Gambler, Flux Capacitor, Martyr, Bounty, Jester 1.
- Gained from coin effects: Copper (Tails +1), Spark (Heads +2), Flux Capacitor (Tails +1).
- A coin you cannot pay for cannot be flipped. Discard it, or save energy.

## 9. The shop

Opened after a level ends. Everything is seeded.

| Section | Details |
|---|---|
| **Coins** | 4 distinct offers from *all* coins of your character (starting pool and locked ones). Normal costs 5, everything else 15. A deck can only hold as many coins as it has slots; when it is full you must buy a slot or remove a coin. Buying a locked coin unlocks it permanently for your coin sets. |
| **Deck slots** | The deck starts with 5 slots and grows up to 10. Each extra slot costs **5 gold** and is bought by clicking the first dark slot in your deck strip (it shows `+5 GOLD`); the others stay locked until the one before is bought. A bigger deck means a bigger quota, so extra slots are a real choice. |
| **Reroll** | Refreshes only the coin offers. 4 gold, +2 for every reroll in this visit. |
| **Chips** | 2 offers from the 7 chips. You can hold at most 3. |
| **Prize** | 1 relic you do not own, 25 gold. |
| **Odds Tuner** | 10 gold: +10% Heads on the selected deck coin. Permanent, capped at 100%. |
| **Coin Removal** | 8 gold: remove the selected deck coin. You never go below 1 coin. |
| **Next Round** | Leaves the shop and starts the next level. |

Select a deck coin by clicking it in the deck strip along the bottom.

## 10. Coin sets (before a run)

- A **set** is up to 5 coins (the starting slots). Each character has 3 sets. Set 1 starts as the default 5-coin deck, sets 2 and 3 are empty
  (an empty set falls back to the default deck).
- At most **3 copies** of one coin, except Normal, which can fill all 5 slots.
- Only coins from the character's pool or that you unlocked can be added. Unlocking is done by buying the coin in a
  shop during a run.
- Edit sets in Coin Sets, press **Save Set**. Switching set or character drops unsaved changes.

## 11. Controls

| Action | Input |
|---|---|
| Flip / Next Coin | Flip button, or Space |
| Mark a coin | Click it in the bank or opening hand |
| Discard | Discard button (the coin in play; in the opening hand: the marked coins) |
| Use a chip | Click it in the bottom row while a coin is dealt |
| Open shop (quota met) | Open Shop button, top right |
| Menu | Menu button or Esc |
| Debug info | F3 |
| Pick character on the play screen | Side arrows, keys 1 to 3, Left/Right |

## 11a. Saving and continuing

The run is **autosaved** (`run.lua` in the save folder) at the start of every level (the opening hand) and in the shop after every purchase. A level in progress is not saved: quitting in the middle of
one and continuing later restarts that level from its opening hand with the same coins, gold and RNG, so it plays out exactly as it would have. **Continue** on the title menu loads the saved run even after the
game was closed (the run-over and victory screens delete the save). Starting a **New Run** while a save exists asks first. Quit asks only when a level is in progress. The tutorial is never saved. The title screen shows
the version (`v0.2.0 (abc1234)`: number and git commit; `dev` when started from the source folder), and `runs.log` and `crash.log` lines carry it too.

## 11c. Stages (difficulty)

Each character has its own stage, 1 to 8 (`content/stakes.lua`), chosen on the play screen (default: the highest unlocked). A win on the highest unlocked stage unlocks the next one for that character; a profile from before stages starts won characters at stage 2. A stage keeps the rules of the ones below it.

| Stage | Rule |
|---|---|
| 1 | no changes |
| 2 | quotas +15% |
| 3 | start with 15 gold |
| 4 | The House (and endless levels) invert every 4th flip |
| 5 | shop prices +20% (coins, chips, prizes) |
| 6 | only 2 exchanges per level |
| 7 | level 1 has a modifier too |
| 8 | quotas +80% in total |

The simulator shows the boss clear rate (smart bot, built set) falling from about 49% (stage 1) to 25% (stage 8) for the Blade; stages 3 to 6 matter more to people than to the bots, which barely use gold or exchanges. `lua tools/sim.lua --stake N` plays a stage.

## 11b. Characters unlock

The Blade is open from the start. **Winning a run** (beating The House) unlocks the next character: Blade, then Seer, then Trader. Locked characters are greyed out on the play screen
("LOCKED - win a run with: The Blade") and cannot be started or edited in Coin Sets. The victory screen announces the unlock. The best number of **endless levels** per character
is saved and shown on the play screen and the run-over screen ("NEW RECORD!"). Clear Progress resets all of it.

## 12. Worked example

Blade, default deck: 4 Normal + Sword, level 1 (quota 3 with 5 coins).

- Opening hand shows Normal, Sword, Normal, Normal, Normal. You keep all (a discard would lower your flips below the quota).
- Flip 1, Normal: Heads, +1 point (1/3). Flip 2, Sword: Tails, nothing. Flip 3, Normal: Heads (2/3). Flip 4, Normal: Tails.
- Last coin, Normal: Tails. Stack is empty at 2/3. You have 25 gold, so a notice offers an exchange for 10 gold; paying
  returns three played coins. Each has a good chance to land Heads for the missing point (a 50% coin three times: 87.5%),
  and the level is cleared for +25 gold.
- The shop opens: Sword is 15, Normal 5. A 10-coin deck would raise the next quota to 9 and needs 5 more slots, so you buy one slot and one coin and
  an odds tuner instead.
