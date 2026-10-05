# Tossup — complete game and code reference

A single-player roguelike about flipping coins, built with Lua 5.1-compatible Lua on LÖVE 2D (11.x, LuaJIT).
You build a bank of coins, flip them one at a time, and score points against a per-level **quota**. There is no
player HP: coins score points, and "penalty" effects raise the quota instead. This file is the full
reference for the design, the rules as implemented, all content, and the code. Paths are relative to the repo
root. Run the game with `love .`.

The UI is available in English and German (`src/lang.lua`, `locales/de.lua`; option "Language").
If something here disagrees with the code, the code wins (then fix this file).
Longer, topic-by-topic documentation (design, gameplay, coins, characters, items, balance, UI, architecture) is in [docs/](docs/README.md).

---

## 1. Rules (as implemented)

### 1.1 Run structure
- A run has **4 levels** (`Game.route` in `src/game.lua`). There is **no draw budget**: a level lasts exactly as
  long as the coins in your stack (every coin is played once, no reshuffle). The **quota scales with the number
  of coins in your deck** at level start: `quota = round(per_coin × deck size)` (`Game.quota_for`).

<!-- GEN:spec-route -->
| # | Name | Quota per coin | Quota with 5 / 10 coins | Payout (gold) |
|---|---|---|---|---|
| 1 | Opening | 0.7 | 4 / 7 | 25 |
| 2 | Second Chance | 1.4 | 7 / 14 | 30 |
| 3 | High Stakes | 2.5 | 13 / 25 | 20 |
| 4 | Rising Tide | 3.2 | 16 / 32 | 35 |
| 5 | Double Down | 3.9 | 20 / 39 | 40 |
| 6 | Last Call | 4.5 | 23 / 45 | 45 |
| 7 | Final Table | 5.2 | 26 / 52 | 50 |
| 8 | The House (boss) | 6 | 30 / 60 | none |
<!-- /GEN:spec-route -->

- You **win the run** when the boss quota is met (then Endless Mode may continue it: `Game.continue_endless`, `Game.stage(level)` generates levels 5+ with 0.5 more quota per coin each, inverting every 5th flip). You **lose the run** if the stack runs out (and no exchange is
  taken) before a level's quota is met (phase `GAME_OVER`). Between levels you visit the **shop**.
- Start of run: gold 25 (`Game.START_GOLD`), energy 3 (max 3), no items, no relics, deck = the chosen coin set.
- Phases (`game.phase`): `ENCOUNTER` (a level, including the opening-hand step), `SHOP`, `VICTORY`, `GAME_OVER`.
- Everything random uses one seeded RNG (`src/rng.lua`, minstd LCG). Same seed + same actions = same run.
  Never call `math.random` in rules code; use `RNG.random(game)` / `RNG.int(game, a, b)`.

### 1.2 Coins, odds and effects
- Every coin instance in your deck is `{uid, id, bonus, ...}` (`bonus` is the permanent Heads boost from the
  shop's odds tuner, +0.03 each). Free-form fields (e.g. `stack`, `charge`) may be added by hooks and persist for
  the run.
- **Heads probability** = coin base `probability` + instance `bonus` + level odds bonus (from `probability`
  effects and the Magnet relic) + `on_odds` hook adjustments, clamped to 0..1 (`Game.probability`).
- A coin has a `heads` and a `tails` list of **effects**. Effect types:
  - `score` — adds points (reduces remaining quota; counts toward `encounter.scored`).
  - `gold` — adds gold.
  - `energy` — adds energy.
  - `penalty` — raises the quota (both remaining and total) by amount.
  - `extra_draw` — a played coin goes back into the draw pile (the flipping coin itself, or a random played
    coin when used from an item), so it can be played again. Max `Game.RETURN_CAP` (3) per level.
  - `probability` — +X Heads chance for that coin for the rest of the level.
  - `all_odds` / `peek` / `extra_exchange` — all coins +X Heads for the level / look at the next two coins / one more exchange this level.
  - `amplify` — every active buff lasts one coin longer and gets stronger.
  - `combo_bonus` / `combo_shield` — the combo (same result in a row; points and gold x(1 + 0.25 per extra step), cap x3, state in `encounter.combo_*`): extra steps / one ignored break.
  - `next_mult` / `next_odds` / `next_swap` / `next_heads` — buffs for the next N coins (`effect.coins`): x factor on points and
    gold, +X Heads, use the other side's effects, guaranteed Heads. Stored in `encounter.buffs`; `Game.add_buff`.
- Coins may have `energy_cost` (energy paid to flip; default 0).

### 1.3 A level, step by step
1. **Start of level** (`start_encounter`): the quota is computed from the deck size; energy refills to max; the
   draw pile is the shuffled deck; hooks' `grow(inst, "level")` run for every owned coin; `encounter_start` is
   emitted; a **mulligan hand** of up to 5 coins is drawn from the pile.
2. **Opening hand (mulligan):** the player marks coins and discards them (free). Discarded coins stay out for the
   level; at least one coin must be kept. Then the kept coins become the **bank**. (Headless/tests: pass no
   `manual_mulligan` to `Game.new` and the hand is kept automatically.)
3. **Bank:** an ordered list; the UI shows the next **3** (`Game.VISIBLE`). The first coin is the **dealt**
   coin. Whenever fewer than 3 coins are in the bank, it refills from the draw pile. When the pile is empty it is
   NOT reshuffled: the stack (bank + pile) only ever shrinks, except through "extra draw" returns and exchanges.
   (`game.reshuffle = true` re-enables reshuffling; used only by tests and the coin-strength simulator.)
4. **Discard** (free): after the opening hand only the dealt coin can be discarded (`Game.discard(game)`; the bank cards are not clickable). Not possible mid-flip, during the
   mulligan, or if it would leave no usable coin. Discard hooks (`on_discard`, `grow(inst,"discard")`) run.
5. **Flip** (`Game.flip`): allowed if the dealt coin's energy cost is paid (`Game.can_flip`). A coin you cannot
   afford cannot be flipped (discard it). Exception: the last usable coin always flips (pays what it can).
   Order of the flip pipeline:
   1. pay energy; the coin leaves the bank at once and the bank refills;
   2. raw roll with the seeded RNG against the coin's current probability → `pending.raw`;
   3. `coin_flip` event: coin hooks `on_flip`, and armed items (Force Heads/Tails) may change `pending.result`;
   4. `finalize`: `coin_outcome` event (relics, e.g. Lucky Penny, Broken Clock, may change the side), then the
      boss rule (every 5th flip of the boss level inverts the side). The final side is `pending.result`.
      `pending.altered` records "RELIC"/"THE HOUSE" for the UI.
   The UI animates a spin that lands on that final side, then holds ~0.9 s.
6. **Resolve** (`Game.resolve`): copies the final side's effect list; `coin_resolve` event (hooks `on_resolve`,
   Double Down can edit the list); applies each effect (`effect_applied` event each); `coin_resolved` event;
   `grow(inst,"flip")`; logs; checks the level state (below); deals the next coin.
7. **Quota met** (`encounter.quota <= 0`): the level is marked `cleared` once: `game.cleared += 1` and the level
   **payout** is given immediately. The level **stays open**:
   - every 2 points beyond the quota pay 1 gold (`Game.SURPLUS_RATE = 0.5`, floor of the running total);
   - the player may keep flipping, or press **Open Shop** (`Game.end_level`) whenever no flip is pending;
   - when the stack runs dry after clearing, the shop is opened automatically (unless an exchange is possible,
     then the player chooses);
   - on the **boss**, meeting the quota ends the run at once (`VICTORY`).
8. **Empty stack** (`Game.stack_empty`, called when nothing can be dealt): if the quota is unmet and an
   **exchange** is possible, `game.exchange_open` is set and the player chooses; otherwise the level is lost.
   - **Exchange** (`Game.exchange`): pay gold (`Game.exchange_cost` = `EXCHANGE_BASE` 10 + `EXCHANGE_STEP` 5 per
     exchange already made this level) to put up to `EXCHANGE_GAIN` (3) of the coins you already played this
     level (seeded random; discarded coins never return) back into the stack. Needs enough gold and at least one
     played coin. Can be repeated while affordable, at most `Game.EXCHANGE_MAX` (3) times per level; after that an empty stack with the quota unmet loses the level.
   - **Give up** (`Game.give_up`) loses the level instead (only with an empty stack and an unmet quota).
9. `encounter_end` is emitted when the level really ends (`end_level` or loss). Armed item effects are cleared.

### 1.4 Energy
- Start of every level: energy = max energy (3, no way to grow it now).
- Spent only on flipping coins that have an `energy_cost`. Gained from coin effects (`energy`).
- `Game.reroll` / `Game.force` (energy-based) still exist in `src/game.lua` but **nothing calls them** (dead code
  kept for tests).

### 1.5 Shop (phase `SHOP`)
Stocked by `enter_shop` after a level ends. Everything random uses the seeded RNG.
- **Coin offers:** 4 distinct coins drawn from **all coins of the character** (starting pool plus locked ones).
  Price = coin `cost` (default 15; Normal 5). Buying is refused if the deck has no free slot (`#coins >= game.slots`; slots start at `Game.START_MAX` = 5, up to `Game.DECK_MAX` = 10). Buying a coin not yet unlocked marks it in `game.purchased`; the UI then unlocks it permanently
  (`Profile.grant`).
- **Deck slots:** `Game.buy_slot(game)` costs `Game.SLOT_COST` (5 gold) per slot, up to `DECK_MAX`; the shop UI sells them as the dark slots of the deck strip.
- **Reroll** the coin offers: 4 gold, +2 per reroll within one visit.
- **Items ("Chips"):** 2 distinct offers from the 7 items; max 3 held (`Items.MAX`).
- **Relic ("Prize"):** 1 offer, a relic you don't own; price 25.
- **Tune-ups for the selected deck coin:** Odds Tuner (10 gold, +0.03 Heads to that coin instance, permanent,
  capped at 100%); Coin Removal (8 gold, never below 1 coin).
- Bought chips and prizes appear in the shop's owned panel, with their effects on hover.
- Leave with **Next Round** (`Game.leave_shop`) → next level.

### 1.6 Items (consumables, used mid-level while a coin is dealt)
Used from three slots next to the Flip button. Only usable when a coin is dealt and not flipping. An item may
refuse (it is then not consumed). One-shot item effects are armed on the event bus (`Items.arm`) and cleared when
the level ends. Costs are in gold in the shop.

### 1.6b Level modifiers (`content/modifiers.lua`)
From level 2 on every level has one, picked with the seeded RNG at level start (`encounter.modifier`): lucky_day (+10% Heads), cold_snap (-10% Heads, payout +40%), power_surge (5 energy), blackout (1 energy, payout +40%), gold_rush (gold x2), high_stakes (quota +25%, payout +50%), good_rhythm (combo step 0.4), bonus_exchange (+1 exchange). `Game.use_modifiers = false` switches them off (tests).

### 1.7 Relics (passive for the run)
Subscribe to bus events through `register(ctx)` while owned (`src/relics.lua`). Bound per game.

### 1.8 Deck and coin sets
- A **coin set** = up to `Game.START_MAX` (5) coins (older longer sets are cut on load, `Profile.SET_SIZE`).
  Per set, common coins allow 3 total, uncommon 2 total, rare 1 total and epic 1 total (`Profile.rarity_limit`). Normal counts as common.
  Each character has 3 sets (`Profile.SET_COUNT`). Set 1 starts as the character's default deck; sets 2 and 3 start empty (an empty set falls back to the default).
- A run starts with the set selected on the play screen. During a run the deck can only grow through the shop (buy a slot, then a coin),
  up to 10 coins.
- Coins can be added to a set only if they are in the character's **starting pool** or **unlocked**.
- **Unlocking:** buy the coin in the shop during a run. There is no token-based unlocking any more.

---

## 2. Content

### 2.1 Coins (`content/coins/<id>.lua`, order in `content/coin_order.lua`)
Columns: rarity (N common, R uncommon, SR rare, UR epic), base Heads %, shop cost, energy cost to flip, Heads
effect, Tails effect, hooks used. "quota +N" = a penalty effect.

<!-- GEN:spec-coins -->
| id | Name | Rar | Heads | Cost | Energy | On Heads | On Tails | Description | Hooks |
|---|---|---|---|---|---|---|---|---|---|
| `normal` | Normal | N | 65% | 5 | 0 | Score 1 point | nothing | A plain coin. Barely a scratch. | - |
| `copper` | Copper | N | 30% | 10 | 0 | Gain 2 gold | Gain 1 energy | Funds your next move. | - |
| `sword` | Sword | N | 35% | 12 | 0 | Score 3 points | nothing | Steady points on Heads. | - |
| `lucky` | Lucky | N | 30% | 10 | 0 | Score 2 points, Goes back into the pile | nothing | Heads: 2 points, and it goes back into the pile to play again. | - |
| `cursed` | Cursed | SR | 25% | 15 | 0 | Score 15 points | Quota +2 | A powerful, dangerous wager. | - |
| `loaded` | Loaded | N | 59% | 12 | 0 | Gain 4 gold | gold_loss | Heads: 4 gold. Tails: lose up to 8 gold. Edge: lose up to 4, then gain 2 gold. | - |
| `dagger` | Dagger | N | 67% | 12 | 0 | Score 2 points | Score 1 point | Scores either way. | - |
| `compost` | Compost | N | 47% | 10 | 0 | Score 2 points | Quota +2, fortune_odds | Tails: quota +2; once per level, Fortune coins gain +11% Heads for the run (max +55%). | - |
| `square_dance` | Square Dance | N | 27% | 14 | 0 | nothing | nothing | Heads: 2 points times the square of Square Dance copies in your deck. | on_resolve |
| `hammer` | Hammer | R | 25% | 22 | 2 | Score 16 points | Score 1 point | A rare but crushing hit. | - |
| `whetstone` | Whetstone | R | 67% | 22 | 0 | type_buff | nothing | Heads: next 2 Steel coins gain 3 points on Heads, but add 2 quota on Tails. | - |
| `blood` | Blood | R | 37% | 22 | 1 | Score 10 points | Quota +6 | Heads: 10 points. Tails: quota +6. Edge: half of both. | - |
| `spark` | Spark | R | 70% | 9 | 0 | Gain 2 energy | Score 3 points | Energy or a small strike. | - |
| `focus` | Focus | R | 65% | 10 | 0 | Next coin: +35% Heads | Score 4 points | Heads: the next coin gets +35% Heads. Tails: 4 points. | - |
| `snowball` | Snowball | SR | 30% | 24 | 1 | Score 3 points | Score 1 point | Heads gains +1 point every flip for the whole run (max +8). | on_resolve, grow |
| `gambler` | Gambler | SR | 35% | 22 | 1 | Score 7 points | nothing | Heads is a bet: 50% triple points, otherwise nothing. | on_resolve |
| `momentum` | Momentum | SR | 35% | 15 | 0 | Score 6 points | nothing | +5% Heads for every Heads in a row this level. | on_odds |
| `echo` | Echo | UR | 60% | 15 | 0 | nothing | nothing | Repeats the effects the previous coin had for this side. | on_resolve |
| `vampire` | Vampire | R | 35% | 15 | 0 | Score 2 points | nothing | Heads: 2 points and it drains 2 gold from the house. | on_resolve |
| `miser` | Miser | R | 40% | 15 | 0 | nothing | Gain 2 gold | Heads: 1 point per 10 gold you hold. | on_resolve |
| `counterfeiter` | Counterfeiter | R | 61% | 25 | 0 | type_buff | nothing | Heads: next 2 Greed coins double all gold gained. Each Tails also loses up to 3 gold. | - |
| `fuse` | Fuse | SR | 30% | 10 | 0 | Score 1 point | nothing | Discard it to charge +6. Heads spends all charge as points. | on_discard, on_resolve |
| `phoenix` | Phoenix | UR | 25% | 15 | 0 | Score 3 points | Quota +2 | Each Tails stores anger (max 5). Heads: 3 points +2 per anger. | on_resolve |
| `contrarian` | Contrarian | SR | 65% | 15 | 0 | Score 4 points | Score 1 point | Always lands opposite of the previous flip. | on_flip |
| `chain` | Chain | R | 60% | 15 | 0 | nothing | nothing | Heads: 2 points per Heads in a row, including this one. | on_resolve |
| `bank` | Bank | R | 40% | 15 | 0 | Gain 3 gold | nothing | Heads: +3 gold, plus 1 per 10 gold held (max +3). | on_resolve |
| `lucky_seven` | Lucky Seven | SR | 30% | 15 | 0 | Score 3 points | nothing | 1 in 7: lands Heads and pays triple points. | on_flip, on_resolve |
| `hourglass` | Hourglass | R | 30% | 10 | 0 | Score 4 points | Goes back into the pile | +30% Heads when 3 or fewer coins are left. Tails: goes back into the pile. | on_odds |
| `capacitor` | Flux Capacitor | R | 35% | 15 | 1 | nothing | Gain 1 energy | Heads: 2 points per energy you hold. Tails: +1 energy. | on_resolve |
| `martyr` | Martyr | SR | 40% | 15 | 1 | Score 4 points | Quota +3 | Tails: quota +3. Heads: 4 points, +1 per Tails so far this level. | on_resolve, grow |
| `blood_pact` | Blood Pact | SR | 58% | 32 | 0 | type_buff | nothing | Heads: next Blood coin gains 8 points on Heads or adds 4 quota on Tails. Edge gets half of both. | - |
| `bounty` | Bounty | SR | 40% | 15 | 1 | Score 4 points | Score 2 points | Pays 1 gold for every 2 points it scores. | register |
| `jester` | Jester | UR | 50% | 15 | 1 | nothing | nothing | Heads or Tails, it does something random. | on_resolve |
| `doppelganger` | Doppelganger | SR | 53% | 34 | 0 | type_buff | nothing | Heads: next Chaos coin applies its resolved effects twice, including penalties. | - |
| `flock` | Flock | R | 25% | 15 | 0 | Score 4 points | nothing | +10% Heads for every other Flock in your deck. | on_odds |
| `megaphone` | Megaphone | R | 65% | 20 | 1 | Score 2 points, Next 2 coins pay x2 | nothing | Heads: 2 points, and the next 2 coins pay double. | - |
| `cheerleader` | Cheerleader | R | 70% | 15 | 0 | Score 2 points, Next 2 coins: +20% Heads | Next coin: +20% Heads | Heads: 2 points, next 2 coins +20% Heads. Tails: next coin +20%. | - |
| `mirror` | Mirror | SR | 65% | 15 | 0 | Score 1 point, Next coin uses its other side | Score 2 points | Heads: the next coin uses the effects of its other side. Tails: 2 points. | - |
| `twin` | Twin | SR | 65% | 15 | 0 | Score 4 points | Score 1 point | Always lands the same as the previous flip. | on_flip |
| `pot` | Pot | R | 25% | 15 | 0 | nothing | Score 1 point | Heads: points equal to the flips made so far this level (max 8). | on_resolve |
| `domino` | Domino | UR | 60% | 15 | 0 | Score 2 points, Next coin lands Heads | Quota +1 | Heads: 2 points, and the next coin lands Heads. Tails: quota +1. | - |
| `hot_hand` | Hot Hand | R | 70% | 15 | 0 | Score 2 points, Combo grows 1 extra step | nothing | Heads: 2 points, and the combo grows by 1 extra step. | - |
| `anchor` | Anchor | R | 70% | 15 | 0 | Score 2 points, The next combo break is prevented | Score 1 point | Heads: 2 points, and the next time the combo would break it holds instead. | - |
| `bettor` | Bettor | SR | 35% | 15 | 1 | nothing | Quota +2 | Heads: 3 points per flip in the current combo (max 30). Tails: quota +2. | on_resolve |
| `cash_out` | Cash Out | SR | 30% | 15 | 0 | Score 3 points | nothing | Heads: 3 points, squares the combo multiplier, banks its pot, then resets the combo. | on_resolve |
| `cold_streak` | Cold Streak | R | 20% | 15 | 0 | Score 1 point | nothing | Tails: 2 points per Tails in a row (max 20). Heads: 1 point. | on_resolve |
| `amplifier` | Amplifier | SR | 70% | 15 | 1 | Score 1 point, Buffs last 1 coin longer and get stronger | Score 1 point | Heads: 1 point. Active buffs last 1 coin longer; odds and multipliers grow stronger. Tails: 1 point. | - |
| `true_echo` | True Echo | UR | 50% | 15 | 0 | nothing | nothing | Repeats what the previous coin really did, including its buffs and growth, on either side. | on_resolve |
| `doubler` | Doubler | SR | 30% | 15 | 0 | nothing | nothing | Heads: 3 points, doubled for every Doubler flip so far this level (3, 6, 12, 24... up to 384). | on_resolve |
| `jackpot` | Jackpot | SR | 15% | 18 | 1 | Score 25 points | nothing | Heads: 25 points. Only 15% Heads. | - |
| `mimic` | Mimic | UR | 30% | 15 | 0 | nothing | Score 1 point | Heads: does what the Heads side of a random other coin in your deck does. | on_resolve |
| `good_dog` | Good Dog | UR | 43% | 28 | 0 | Score 2 points, fetch_best | nothing | Heads: 2 points; return the highest-scoring coin played this level to the draw pile. Once per level. | - |
| `orchestra` | Orchestra | R | 25% | 15 | 0 | nothing | Gain 1 gold | Heads: 2 points per different coin type in your deck. | on_resolve |
| `conductor` | Conductor | R | 73% | 24 | 0 | type_buff | nothing | Heads: next 3 Rhythm coins gain 2 points per combo step (max 8); a broken combo adds 5 quota. | - |
| `lifeline` | Lifeline | R | 70% | 15 | 0 | Score 1 point, One more exchange this level | nothing | Heads: 1 point, and you may exchange one more time this level. | - |
| `horoscope` | Horoscope | R | 70% | 15 | 0 | Score 1 point, All coins +7% Heads this level | All coins +3% Heads this level | Heads: 1 point, all coins +7% Heads this level. Tails: all coins +3%. | - |
| `crystal_ball` | Crystal Ball | SR | 30% | 15 | 0 | Score 5 points | Discard one of the next three coins | Heads: 5 points. Tails: discard one of the next three coins. | - |
<!-- /GEN:spec-coins -->

(The strength table of coins is reproducible with `lua tools/sim.lua --coins`.)

### 2.2 Items (`content/items/<id>.lua`)

<!-- GEN:spec-items -->
| id | Name | Cost | Effect |
|---|---|---|---|
| `double_down` | Double Down | 14 | Next coin: points, gold, energy and penalties x2 |
| `energy_drink` | Energy Drink | 8 | Gain 2 energy |
| `extra_draw` | Extra Draw | 10 | A played coin returns to the pile |
| `force_heads` | Force Heads | 12 | Dealt coin lands Heads |
| `force_tails` | Force Tails | 12 | Dealt coin lands Tails |
| `lucky_charm` | Lucky Charm | 10 | The next 2 coins +20% Heads |
| `peek` | Peek | 6 | See the next two coins |
| `safety_net` | Safety Net | 9 | The next combo break is prevented |
| `shortcut` | Shortcut | 12 | Score 3 points at once |
| `swap` | Swap | 8 | Free discard, new coin |
| `weighted` | Weighted | 8 | +25% Heads, one flip |
<!-- /GEN:spec-items -->

### 2.3 Relics (`content/relics/<id>.lua`, price 25)

<!-- GEN:spec-relics -->
| id | Name | Effect |
|---|---|---|
| `baton` | Baton | Combo: +0.4 per step instead of 0.25, up to x4 |
| `clock` | Broken Clock | Every 10th flip is Heads |
| `magnet` | Magnet | Every 3 Heads in a row: +5% Heads this level |
| `metronome` | Metronome | Every 4th flip pays double |
| `penny` | Lucky Penny | First Tails each level becomes Heads |
<!-- /GEN:spec-relics -->

### 2.4 Characters (`content/characters.lua`)
The default sets follow the rarity limits: **Blade** 2 Normal + Sword; **Seer** 2 Normal + Dagger + Focus + Spark;
**Trader** Normal + Loaded + Dagger. "Pool" = usable in coin sets from the start; "Locked" =
unlocked by buying in the shop. The shop sells pool and locked coins alike.

<!-- GEN:spec-characters -->
| id | Name | Pool (usable from the start) | Locked (unlock by buying) |
|---|---|---|---|
| `blade` | The Blade - "Reliable points" | normal, sword, dagger | hammer, blood, vampire, chain, cursed, fuse, focus, martyr, snowball, spark, jackpot, lifeline, megaphone, pot, hot_hand, cash_out, doubler, amplifier, compost, square_dance, good_dog, whetstone, blood_pact, conductor |
| `seer` | The Seer - "Risk and changing odds" | normal, dagger, cursed, gambler, spark, focus, lucky, compost | horoscope, crystal_ball, mimic, contrarian, lucky_seven, blood, hourglass, jester, echo, phoenix, mirror, domino, twin, cold_streak, anchor, true_echo, amplifier, square_dance, good_dog, blood_pact, doppelganger |
| `trader` | The Trader - "Gold and energy" | normal, copper, loaded, dagger, sword, square_dance | spark, bank, miser, hammer, bounty, flock, momentum, capacitor, cheerleader, megaphone, orchestra, lifeline, jackpot, bettor, anchor, doubler, true_echo, compost, good_dog, counterfeiter, conductor |
<!-- /GEN:spec-characters -->

(The second number in each `locked` entry is unused leftover from the old token system.)

---

## 3. Meta progression, menus and screens

### 3.1 Profile (`src/profile.lua`, saved as `profile.lua` in the LÖVE save folder, identity `tossup`)
Pure data + serialization (no LÖVE). Fields: `tokens` (earned, **currently unused**), `unlocked[char][coin]`,
`collected[coin]` (coins that have been in a deck, for the Collection screen), `sets[char] = {{name, coins}, ...}`
(3 sets), `active_set[char]` (the set the play screen opens on), `options`. Loading fills missing fields and
migrates an old single `loadouts` list into set 1. `Profile.loadout(profile, char, max)` returns the
coins a run starts with: the selected set filtered to available coins, capped at `max`, with the rarity copy limit;
an empty set falls back to the character's default deck.

Tokens: `Game.run_tokens` = levels cleared + 3 for a won run. They are earned and saved but nothing spends them.

### 3.2 Screens (`src/ui/views/`)
- `title` — menu: Continue / New Run (if a run is paused) or Play, Coin Sets, Collection, Options, Quit.
- `menu` (play screen) — pick a character with the side arrows (or keys 1–3, Left/Right), shows the selected coin
  set (cycle with its arrows), "Edit Coin Sets", Start Run.
- `sets` — Coin Sets editor: tabs for every character; 3 set tabs; a 10-slot set; the character's coins (locked
  ones show a padlock). Edits are a draft; **Save Set** writes them (switching set/character drops unsaved edits).
- `collection` — grid of all coins (5×3 per page), sort (Rarity/Name/Default) and rarity filter tabs
  (All/Common/Uncommon/Rare/Epic ↔ N/R/SR/UR). Uncollected coins are black silhouettes.
- `options` — screen shake, fast flip (0.8 s instead of 1.6 s animation), fullscreen.
- `encounter` — the level: header (points/quota bar, coins left, gold, energy, Open Shop when cleared), coin bank
  (left, next 3 coins, display only), centre stage (the big coin, its Heads and Tails effect boxes, odds bar,
  result banner), bottom row (Discard, Flip/Next Coin, 3 item slots). The opening hand floats in the centre of a dimmed
  screen. When the stack is empty and an exchange is possible a notice offers Buy More Coins / Start Again / Back To Menu.
- `shop` — full-screen shop (see 1.5).
- `finish` — run over / victory screen (full screen like the shop).

The game draws on a fixed **1280×800 canvas** scaled and centred to fit the window (felt-green backdrop around it;
`ui.mouse()` / `ui.to_canvas()` convert window → canvas coordinates; use them instead of
`love.mouse.getPosition()`).

---

## 4. Code architecture

```
main.lua                  forwards LÖVE callbacks to src/ui/app.lua
conf.lua                  window 1280x800, identity "tossup"
src/game.lua              ALL rules (no LÖVE dependency): Game.new/flip/resolve/discard/shop/...
src/rng.lua               seeded minstd RNG (RNG.random/int on the game table)
src/signal.lua            tiny mutable event bus: Signal.on/off/emit
src/hooks.lua             routes a coin's hooks onto the bus, scoped to the coin in play
src/relics.lua            binds owned relics to the bus
src/items.lua             item use/arming (Items.use, Items.arm, Items.MAX = 3)
src/profile.lua           saved meta progression (pure)
content/coins.lua         coin registry (requires content/coin_order.lua + content/coins/<id>.lua)
content/items.lua, content/relics.lua   registries (+ content/items/*.lua, content/relics/*.lua)
content/characters.lua    characters: deck, pool, locked
src/ui/app.lua            load/draw/update/input, screen dispatch
src/ui/state.lua          shared UI state (screen, marks, hover, draft set, ...)
src/ui/actions.lua        player actions + per-frame flow (flip animation → hold → resolve), profile saves
src/ui/draw.lua, theme.lua  drawing primitives, tooltips, palette
src/ui/views/*.lua        one file per screen
tests/                    test_game / test_hooks / test_items / test_bank / test_profile (plain Lua, no LÖVE)
tools/sim.lua             headless balance simulator;  tools/gen_coin_icons.py  coin art generator (Pillow)
tools/gen_ui_icons.py     item, relic and UI icons (next round button, reroll, gold, energy, coins left, open shop, exchange, give up, flip, discard, next coin, start level, the SHOP title)
cli.lua                   `lua cli.lua <seed> <character>` plays one automated run and prints the log
```

Layers: rules (`src/game.lua` and friends, testable headless) → state/actions → views (draw only, no game logic).
Do not put gameplay logic in views, do not use `math.random`, and keep the rules free of LÖVE APIs.

Key `Game` API (all take the `game` table first): `Game.new(seed, character, unlocked, loadout, manual_mulligan)`, `coins_left`, `quota_for(level, coins)`, `can_exchange`, `exchange`, `exchange_cost`, `give_up`,
`mulligan_discard(game, uids)`, `mulligan_done`, `flip`, `resolve`, `discard(game, uids)`, `can_flip`,
`flip_cost`, `end_level`, `leave_shop`, `buy(game, index)`, `buy_item`, `buy_relic`, `reroll_shop`, `upgrade`,
`remove`, `use_item(game, slot)`, `add_relic`, `probability(game, coin)`, `run_tokens`, `get_coin`, `apply_effect`,
`log`. Constants: `Game.VISIBLE=3`, `MULLIGAN=5`, `START_MAX=5`, `DECK_MAX=10`, `SLOT_COST=5`,
`SURPLUS_RATE=0.5`, `EXCHANGE_BASE=10`, `EXCHANGE_STEP=5`, `EXCHANGE_GAIN=3`, `EXCHANGE_MAX=3`, `RETURN_CAP=3`. `Game.route` is the mutable level table (the simulator overrides it).

Important state: `game.coins` (deck), `game.encounter` (`quota`, `max_quota`, `scored`, `pile`, `queue`,
`discarded`, `played`, `returned`, `cleared`, `flips`, `streak`, `bonus`, `magnet`, `boss`, ...), `game.mulligan` (`{hand}` or nil), `game.exchange_open`,
`game.dealt` (`{uid, probability}`), `game.pending` (the coin being flipped: `raw`, `result`, `altered`, `gained`),
`game.last_result`, `game.items`, `game.relics`, `game.purchased`, `game.log`.

### 4.1 Hooks (coins) — `src/hooks.lua`
A coin file returns `{name, description, rarity, probability, cost?, energy_cost?, heads = {effects}, tails =
{effects}, <hooks>}`. Hooks (all optional):
- `on_deal(game, inst)`; `on_flip(game, inst, flip)` (set `flip.result`); `on_resolve(game, inst, res)` (edit
  `res.effects`, a private copy; `res.result` is the side); `on_discard(game, inst)`;
- `on_odds(game, inst, odds)` — **pure**, evaluated whenever odds are shown, for every owned coin; mutate `odds.p`;
- `grow(inst, event)` — `"level"` (every owned coin at level start), `"flip"`, `"discard"`; `inst` persists for
  the whole run, so counters live on it;
- `register(ctx)` with `ctx = {game, inst, on}` — subscribe to bus events yourself; torn down when the coin
  resolves or is discarded.
Rules for hook code: use `RNG` for randomness; require `src.game` lazily inside a hook, never at the file top
(it loads the coin files); effects are plain tables `{type=..., amount=...}`.

### 4.2 Bus events (`Signal`)
`coin_deal`, `coin_flip{flip}`, `coin_outcome{flips, result}` (relics change `result`), `coin_resolve{res}`,
`effect_applied{effect, text}`, `coin_resolved{res}`, `coin_discard`, `encounter_start{encounter}`,
`encounter_end{won}`. Scoped events carry `{game, inst}`.

### 4.3 Items and relics
- Item file: `{name, short, description, cost, use = function(game, Items) ... end}`; return `false` to refuse.
  Use `Items.arm(event, fn)` for one-shot effects on a later event.
- Relic file: `{name, description, register = function(ctx) ctx.on(event, handler) end}`.

---

## 5. Adding content (checklist)
- **New coin:** create `content/coins/<id>.lua`; add the id to `content/coin_order.lua`; add a colour and an emblem
  to `tools/gen_coin_icons.py` and run `python3 tools/gen_coin_icons.py` (the game loads `assets/coins/<id>.png`
  for every coin and crashes without it); add it to a character's `pool` or `locked` list in
  `content/characters.lua`; add tests if it has a hook (see `tests/test_hooks.lua`); run
  `lua tools/sim.lua --coins` to see how strong it is.
- **New item / relic:** add the file, add the id to `content/items.lua` / `content/relics.lua` (the ORDER list),
  and add a colour and an emblem to `tools/gen_ui_icons.py`, then run `python3 tools/gen_ui_icons.py` (the game
  loads `assets/items/<id>.png` / `assets/relics/<id>.png` for every item and relic and crashes without them).
- **New effect type:** add a branch in `apply_effect` (`src/game.lua`), the labels in `src/ui/draw.lua`
  (`effects`, `effect_description`), and coin text.

## 6. Conventions and gotchas
- LÖVE runs **LuaJIT**: there is **no `table.unpack`** (use `unpack` or copy by hand). Plain `lua` (5.4) in tests
  does have it, so such bugs only show up in the real game.
- All randomness through `RNG` for determinism; keep run reproducibility (tests compare logs of identical seeds).
- Views must not call game logic outside `src/ui/actions.lua` helpers; marks (`ui.marked`) are UI-only.
- `box()` draws a drop shadow; the pixel font is `assets/fonts/m6x11plus.ttf` (sizes 16/20/32/48, nearest filter).
- Item/relic/UI icons are 256 px PNGs in `assets/items`, `assets/relics`, `assets/ui` (loaded into `ui.item_images`,
  `ui.relic_images`, `ui.ui_images`; draw with `D.image_at(image, x, y, size)`).
- Look: the shop, the round view and the end screen share the teal "screen" style (`C.screen`, `C.panel_dk`,
  `C.card`, `C.line` in `src/ui/theme.lua`, gold outline); the logo, SHOP title and the screen titles (`title_*.png`) are generated images. Every full-screen view starts with `D.frame(title_image, back_label, back_action)`; Back/Menu sits top right. The mouse cursor is a custom pixel-art arrow (`cursor_arrow.png`; gold `cursor_click.png` while over a button).
- Coin art is 512 px PNGs with mipmaps; drawn at any size with `coin_image(id, x, y, size)`.
- Screenshot tests/scripts that run the game must use a different LÖVE save identity than `tossup` (they would
  otherwise overwrite the real `profile.lua`).

## 7. Testing and the simulator
- `lua tests/test_game.lua`, `test_hooks.lua`, `test_items.lua`, `test_bank.lua`, `test_profile.lua` (all must
  print `... passed`).
- `lua tools/sim.lua` — three bots (random, greedy, smart) play many seeded runs headlessly and print clear rates
  per level. Flags: `--runs N`, `--char`, `--bot`, `--set default` (start from the 5-Normal default deck;
  otherwise a "best coins" 10-coin set), `--unlock all`, `--coins` (per-coin strength table),
  `--quota a,b,c,d` (quota **per coin** per level, e.g. 0.6,1.0,1.6,2.4), `--payout a,b,c`, `--trace SEED` (print one run's log). Bots are crude: use the numbers to find
  broken coins and a broken economy, not to judge fun. Random/greedy bots barely play the shop.

## 8. Current balance and known issues / planned work
- Smart bot, per-coin quotas 0.6/0.9/1.6/3.0, start gold 25, payouts 25/30/35: with a best-coins 10-coin set
  (the sim default): level 1 ≈ 94–100%, level 2 ≈ 84–99%, level 3 ≈ 67–97%, boss ≈ 44–75% (Blade 56, Seer 44,
  Trader 75). With each character's plain default deck (`--set default`): level 1 ≈ 98–100%, level 2 ≈ 54–76%,
  level 3 ≈ 31–41%, boss ≈ 10–15%. Quotas scale with deck size but not with coin
  quality, so deck quality swings results a lot; building a strong set in the editor makes the game much easier.
  Numbers are rough.
- The exchange (10 gold +5 each time, 3 coins back) is a first guess: it makes gold buy extra flips, so with a
  plain 5-Normal start it lifts level 1 from ≈ 51% to ≈ 85% (the starting 10 gold pays for one exchange).
- Tokens are earned/saved but unused; decide what they are for or remove them.
- The surplus-gold rate (1 gold per 2 points beyond the quota) and several coin costs/energy costs are untuned.
- Coin shop prices are almost flat (15); strong coins (Snowball, Flux Capacitor, Hammer, Blood, Gambler, Dagger) are
  under-priced relative to weak ones.
- Dead code: `Game.reroll`, `Game.force` (energy), `Game.buy_energy`.
- The Collection screen has no backdrop art; the shop has no art beyond flat shapes.
- The deck starts with 5 slots; a full deck blocks coin purchases until you buy a slot (5 gold, up to 10) or remove a coin.
