# Coins

Every coin has a complete C# definition in `Assets/Scripts/Content/Coins/<Name>Coin.cs`. A coin has:

- a **Heads chance** (`probability`),
- typed effect lists for **Heads**, **Tails**, and **Edge**,
- optional triggered **Buffs** with a typed target and effect,
- optional **SpecialRule** text for behavior implemented by code hooks,
- an optional **energy cost** to flip,
- a **rarity** (N common, R uncommon, SR rare, UR epic, used for sorting and colour in the Collection screen),
- optional **hooks** that change the rules (see [architecture.md](architecture.md#coin-hooks)).

Outcome rows and declarative Buffs build their hover descriptions from the same data the game applies. Keep hook-only behavior in the hook, and set `SpecialRule` to explain it. Upgrades use `UpgradeChange` entries to add outcome effects, adjust Heads chance, or add a Buff; the upgraded hover uses those same changes.

```csharp
public override IReadOnlyList<Effect> Heads { get; } = [Effect.Score(1)];
public override IReadOnlyList<Effect> Tails { get; } = [Effect.Quota(1)];

public override IReadOnlyList<BuffSpec> Buffs { get; } =
[
    new("chaos_gold", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Chaos),
        Effect.Gold(2), appliesOn: OutcomeSide.Heads)
];
```

This renders the buff as “On Heads, the next Chaos coin gains 2 gold when landing Heads.” A future upgrade can compose another declarative change with the base definition:

```csharp
UpgradeChange.AddEffect(OutcomeSide.Heads, Effect.Score(2));
UpgradeChange.AddHeadsProbability(.06);
```

Prices are in the table below and in each coin heading (generated from the code). "EV" below is the expected value of one flip ignoring hooks, in points unless
stated, with an unmodified Heads chance. A "quota +N" is a penalty that moves the goal posts by N, which is roughly
the same as losing N points.

Sections: [Buffers](#buffers-affect-the-next-coins) | [The baseline](#the-baseline) | [Plain scorers](#plain-scorers) | [Economy and energy](#economy-and-energy) |
[Risk coins](#risk-coins) | [Odds manipulators](#odds-manipulators) | [Growing coins](#growing-coins) |
[Combo and synergy](#combo-and-synergy) | [Wild coins](#wild-coins) | [Strength ranking](#strength-ranking)

---

## All coins at a glance

<!-- GEN:coins-table -->
| Coin | Rarity | Heads | Price | Energy | On Heads | On Tails |
|---|---|---|---|---|---|---|
| Normal | Common | 65% | 5 | 0 | Score 1 point | nothing |
| Copper | Common | 30% | 10 | 0 | Gain 2 gold | Gain 1 energy |
| Sword | Common | 35% | 12 | 0 | Score 3 points | nothing |
| Lucky | Common | 30% | 10 | 0 | Score 2 points, Goes back into the pile | nothing |
| Cursed | Rare | 25% | 15 | 0 | Score 15 points | Quota +2 |
| Loaded | Common | 59% | 12 | 0 | Gain 4 gold | gold_loss |
| Dagger | Common | 67% | 12 | 0 | Score 2 points | Score 1 point |
| Compost | Common | 47% | 10 | 0 | Score 2 points | Quota +2, fortune_odds |
| Square Dance | Common | 27% | 14 | 0 | (see description) | (see description) |
| Hammer | Uncommon | 25% | 22 | 2 | Score 16 points | Score 1 point |
| Whetstone | Uncommon | 67% | 22 | 0 | type_buff | nothing |
| Blood | Uncommon | 37% | 22 | 1 | Score 10 points | Quota +6 |
| Spark | Uncommon | 70% | 9 | 0 | Gain 2 energy | Score 3 points |
| Focus | Uncommon | 65% | 10 | 0 | Next coin: +35% Heads | Score 4 points |
| Snowball | Rare | 30% | 24 | 1 | Score 3 points | Score 1 point |
| Gambler | Rare | 35% | 22 | 1 | Score 7 points | (see description) |
| Momentum | Rare | 35% | 15 | 0 | Score 6 points | (see description) |
| Echo | Epic | 60% | 15 | 0 | (see description) | (see description) |
| Vampire | Uncommon | 35% | 15 | 0 | Score 2 points | (see description) |
| Miser | Uncommon | 40% | 15 | 0 | (see description) | Gain 2 gold |
| Counterfeiter | Uncommon | 61% | 25 | 0 | type_buff | nothing |
| Fuse | Rare | 30% | 10 | 0 | Score 1 point | (see description) |
| Phoenix | Epic | 25% | 15 | 0 | Score 3 points | Quota +2 |
| Contrarian | Rare | 65% | 15 | 0 | Score 4 points | Score 1 point |
| Chain | Uncommon | 60% | 15 | 0 | (see description) | (see description) |
| Bank | Uncommon | 40% | 15 | 0 | Gain 3 gold | (see description) |
| Lucky Seven | Rare | 30% | 15 | 0 | Score 3 points | (see description) |
| Hourglass | Uncommon | 30% | 10 | 0 | Score 4 points | Goes back into the pile |
| Flux Capacitor | Uncommon | 35% | 15 | 1 | (see description) | Gain 1 energy |
| Martyr | Rare | 40% | 15 | 1 | Score 4 points | Quota +3 |
| Blood Pact | Rare | 58% | 32 | 0 | type_buff | nothing |
| Bounty | Rare | 40% | 15 | 1 | Score 4 points | Score 2 points |
| Jester | Epic | 50% | 15 | 1 | (see description) | (see description) |
| Doppelganger | Rare | 53% | 34 | 0 | type_buff | nothing |
| Flock | Uncommon | 25% | 15 | 0 | Score 4 points | (see description) |
| Megaphone | Uncommon | 65% | 20 | 1 | Score 2 points, Next 2 coins pay x2 | nothing |
| Cheerleader | Uncommon | 70% | 15 | 0 | Score 2 points, Next 2 coins: +20% Heads | Next coin: +20% Heads |
| Mirror | Rare | 65% | 15 | 0 | Score 1 point, Next coin uses its other side | Score 2 points |
| Twin | Rare | 65% | 15 | 0 | Score 4 points | Score 1 point |
| Pot | Uncommon | 25% | 15 | 0 | (see description) | Score 1 point |
| Domino | Epic | 60% | 15 | 0 | Score 2 points, Next coin lands Heads | Quota +1 |
| Hot Hand | Uncommon | 70% | 15 | 0 | Score 2 points, Combo grows 1 extra step | nothing |
| Anchor | Uncommon | 70% | 15 | 0 | Score 2 points, The next combo break is prevented | Score 1 point |
| Bettor | Rare | 35% | 15 | 1 | (see description) | Quota +2 |
| Cash Out | Rare | 30% | 15 | 0 | Score 3 points | (see description) |
| Cold Streak | Uncommon | 20% | 15 | 0 | Score 1 point | (see description) |
| Amplifier | Rare | 70% | 15 | 1 | Score 1 point, Buffs last 1 coin longer and get stronger | Score 1 point |
| True Echo | Epic | 50% | 15 | 0 | (see description) | (see description) |
| Doubler | Rare | 30% | 15 | 0 | (see description) | (see description) |
| Jackpot | Rare | 15% | 18 | 1 | Score 25 points | nothing |
| Mimic | Epic | 30% | 15 | 0 | (see description) | Score 1 point |
| Good Dog | Epic | 43% | 28 | 0 | Score 2 points, fetch_best | nothing |
| Orchestra | Uncommon | 25% | 15 | 0 | (see description) | Gain 1 gold |
| Conductor | Uncommon | 73% | 24 | 0 | type_buff | nothing |
| Lifeline | Uncommon | 70% | 15 | 0 | Score 1 point, One more exchange this level | nothing |
| Horoscope | Uncommon | 70% | 15 | 0 | Score 1 point, All coins +7% Heads this level | All coins +3% Heads this level |
| Crystal Ball | Rare | 30% | 15 | 0 | Score 5 points | Discard one of the next three coins |
<!-- /GEN:coins-table -->

---

## Buffers: affect the next coins

These coins act on the coins that come after them, through four "next" effects. A buff made while a coin resolves starts with the
following coin and is used up one coin at a time. The odds shown in the bank include the odds buff, so the numbers on screen are the real
ones. Buffs end with the level.

### Megaphone (R) - 65% - 1 energy - 20 gold
Heads: +2 points and the **next 2 coins pay double** (points and gold). Plays well before a Hammer, Blood or Gambler; plays badly before Normal coins. Use Peek
or the bank to see what follows. Several Megaphones stack: two active buffs are x4.

### Cheerleader (R) - 70%
Heads: +2 points and the next 2 coins get +20% Heads. Tails: the next coin gets +20%. Never a dead flip. Strong in front of low-odds coins such as Hammer (35%)
or Cursed (30%).

### Mirror (SR) - 65%
Heads: +1 point and the **next coin uses the effects of its other side**. Tails: +2 points. Before Blood or Cursed it turns the dangerous Tails into
the big Heads payoff (the effects swap, the roll does not). Before Sword it turns +5 on Heads into nothing, so read the bank first.

### Domino (UR) - 60%
Heads: +2 points and the **next coin lands Heads**. Tails: quota +1. Guaranteed Heads on the next coin turns Cursed (+15) into a sure thing. Relics and the
boss can still change the side afterwards.

### Twin (SR) - 65%
Always lands **the same as the previous flip** (Contrarian is the opposite). Heads +4, Tails +1. Put it behind a coin you made Heads (Domino, Force Heads).

### Pot (R) - 25%
Heads: points equal to the flips made so far this level, this one included (max 12). Tails +1. Worth 1 on the first flip and 8 on the eighth, so keep it for the end
of the stack: do not discard late coins to reach it.

---

## Combo coins

The **combo** (see [gameplay.md](gameplay.md)) multiplies a flip's points by +0.25 for each same result in a row. These coins feed it, spend it or break rules around it.

### Hot Hand (R) - 70%
Heads: +2 points and the combo grows by **1 extra step**, so a streak climbs twice as fast. The cheapest way to reach the x3 cap.

### Anchor (R) - 70%
Heads: +2 points and the **next break is ignored** (a shield, shown as SHIELD 1). Tails: +1 point. The shield makes a gamble such as Cursed safe to flip in the middle of a streak.

### Bettor (SR) - 35% - 1 energy
Heads: **3 points per flip in the current combo** (max 30), and the multiplier applies on top: at a x3 combo (nine in a row) that is 27 x 3 = 81. Tails: quota +2. Flip it deep into a streak, never on a fresh one.

### Cash Out (SR) - 30%
Heads: +3 points, the combo multiplier **counts twice** (x2 becomes x4), then the combo resets. The deliberate finisher at the top of a streak.

### Cold Streak (R) - 20%
Tails: **2 points per Tails in a row** (max 20), Heads +1. The Tails twin of Chain: it makes a Tails streak worth building, and the combo multiplier applies to it too.

---

## Engine coins: they make other coins bigger

### Amplifier (SR) - 70% - 1 energy
Heads: +1 point and **every active buff lasts 1 coin longer and gets stronger** (Megaphone x2 becomes x3, Cheerleader +20% becomes +40%). Tails: +1. Play it
right after a buff coin, in front of the coins you want boosted. Two Amplifiers behind a Megaphone make x4.

### True Echo (UR) - 50%
No effects of its own. It **repeats what the previous coin really did**, whichever side either landed on: Snowball's grown value, Bettor's computed points,
Pot's payout, even the previous coin's buffs (an echoed Megaphone buffs the next two coins again). Echo copies printed values; True Echo copies the result.
Multipliers apply to the copy, so with a x2 combo an echoed 9 pays 18.

### Doubler (SR) - 30%
Heads: 3 points, **doubled for every Doubler flip so far this level**: 3, 6, 12, 24... The counter is shared by all copies, counts Tails flips too and resets each
level. Three copies in one level reach 3 + 6 + 12; with Lucky, Hourglass or Extra Draw bringing copies back the numbers run away (capped at 384 per flip).

---

## Newer coins

### Jackpot (SR) - 15% - 1 energy - 18 gold
Heads: **25 points**. One flip in five. With Cheerleader, Horoscope, Lucky Charm or Domino behind it the odds become real; alone it is a lottery ticket.

### Mimic (UR) - 30%
Heads: does what the **Heads side of a random other coin in your deck** does (seeded; as printed, so Snowball's growth is not copied). Tails: +1 point. Best in a deck of
strong Heads coins; Echo and True Echo copy the *previous* coin, Mimic copies one from the deck.

### Orchestra (R) - 25%
Heads: **2 points per different coin type in your deck** (a deck of five different coins pays 10). Tails: +1 gold. The opposite of Flock: it wants variety.

### Lifeline (R) - 70%
Heads: 1 point and **one more exchange this level** (the limit is 3). A safety coin for decks that run out of coins.

### Horoscope (R) - 70%
Heads: 1 point and **all coins +7% Heads for the rest of the level**; Tails: all coins +3%. It feeds the same pool as Focus and the Magnet relic, so several of them stack.

### Crystal Ball (SR) - 30%
Heads: 5 points. Tails: **discard one of the next three coins of the bank** (free; click the card). A bad Tails turns into a chosen discard, so it pairs with Hourglass or a risky bank. The look at the pile moved to the Peek chip.

---

## The baseline

### Normal (N) - 65% - 5 gold
Heads: +1 point. Tails: nothing. EV 0.5. The only coin that may fill all 5 slots of a set, and the cheapest coin in
the shop. A deck of five Normal coins has a quota of 3 points and an expected score of 2.5, which is why a plain start
needs luck, an exchange, or a first purchase. Normal coins are filler: they keep the quota low (quota scales with deck
size) but they are the weakest use of a flip. Remove them with Coin Removal once the deck fills with better coins.

---

## Plain scorers

### Sword (N) - 35% - 12 gold
Heads +5 points. EV 2.5. Pure reliability: no hook, no cost. The Blade's starter. Four copies of Sword and a few Daggers
beat the early quotas on their own.

### Dagger (N) - 67% - 12 gold
Heads +4, Tails +1. EV 3.25. Scores on both sides and has the best odds of any starter coin. The most efficient plain
scorer in the game. Pairs with every other coin because it never wastes a flip.

### Hammer (R) - 25% - 2 energy - 22 gold
Heads +16, Tails +1. EV 6.25. The strongest raw payoff, but costs two of your three energy, so you can flip at most one per level
without an energy source. Use with Copper, Spark or Flux Capacitor. Poor in a deck that does not generate energy.

### Blood (R) - 37% - 1 energy - 22 gold
Heads +11, Tails quota +6. EV 4.2 net (6.6 points minus 2.4 of quota). Strong but risky: a Tails costs more than half a Heads, so it needs
odds help (Focus, Cheerleader) or a lead. Better when you are ahead and want to finish, worse as a last-flip gamble. Appears in both Blade's and Seer's
locked list.

### Spark (R) - 70% - 9 gold
Heads +2 energy, Tails +3 points. EV 1.5 points and 1.0 energy. Free to flip and feeds expensive coins. Seer's starter
and a Trader and Blade unlock. Always worth a slot next to Hammer, Blood or Snowball.

---

## Economy and energy

### Copper (N) - 30% - 10 gold
Heads +2 gold, Tails +1 energy. EV 1 gold, 0.5 energy. Funds exchanges and the shop; Tails is never a dead flip. Trader's
pool.

### Loaded (N) - 59% - 12 gold
Heads +4 gold. EV 3 gold. The reliable income coin. Gold is also the way to buy a second try (exchange), so Loaded is a
safety net as much as a shop booster. Trader's starter.

### Bank (R) - 40%
Heads +3 gold plus 1 gold per 10 gold held (max +3). EV up to 3 to 3 + interest gold. Rewards hoarding; at 30+ gold it is
effectively +6 per Heads.

### Miser (R) - 40%
Heads: +1 point per 10 gold you hold. Tails +2 gold. At 40 gold that is 4 points per Heads. A rich deck's scorer. Terrible
on a fresh start (0 points) and good late.

### Flux Capacitor (R) - 35% - 1 energy
Heads: +2 points per energy you hold. Tails: +1 energy. With 3 energy (2 after paying the cost) that is 4 points; with 6 it is 10.
Turns spare energy into score. Pairs with Spark and Copper.

### Bounty (SR) - 40% - 1 energy
Heads +4, Tails +2, and every point it scores pays 1 gold per 2 points (a `register` hook on `effect_applied`). EV 3
points and 1.5 gold. A scorer that doubles as income.

### Vampire (R) - 35%
Heads +2 points and +2 gold. EV 1 point and 1 gold. A small, steady two-resource coin. Not strong, but never empty on Heads.

---

## Risk coins

### Cursed (SR) - 25%
Heads +15, Tails quota +2. EV 4.5 points minus 1.4 quota = 3.1 net. A big swing: seven out of ten flips push the goalposts
back. Only worth it with ways to raise the odds (Focus, Magnet, tuners) or to force Heads (chip).

### Gambler (SR) - 35% - 1 energy - 22 gold
Heads +7, then a seeded 50/50: either all effects x3 (21) or nothing. EV 5.25. Pure variance. Excellent when you are behind
and need a jump, wasteful when you are exactly one flip from the quota.

### Phoenix (UR) - 25%
Heads +3, Tails quota +2. Each Tails stores one anger (max 5) on the coin; the next Heads adds +2 points per anger and clears it.
At full anger it scores 13. Wants Tails first, then Heads, so it is best flipped late in the stack when several Phoenixes
have been through Tails. Each copy keeps its own anger for the whole run.

### Martyr (SR) - 40% - 1 energy
Tails quota +3. Heads +4 plus 1 per Tails taken by Martyr this level. The debt resets every level. A slow burn for
decks where Tails is frequent.

### Lucky Seven (SR) - 30%
Each flip has a 1 in 7 chance to force Heads and triple the points (3 x 3 = 9). Otherwise it is an ordinary 40% coin with
+3. EV about 2.3. A small jackpot with a floor.

---

## Odds manipulators

### Focus (R) - 65% - 10 gold
Heads: this coin gains +15% Heads chance for the rest of the level (a `probability` effect). Tails +4 points. EV 2 points + odds
growth. Meant to be flipped more than once with Lucky / Hourglass or tuners; otherwise a weak coin.

### Momentum (SR) - 35%
+5% Heads for every Heads in a row this level (reads `encounter.streak`). Heads +6. After four Heads in a row it is a 60%
coin. Pairs with Chain and Magnet. Streak resets on a Tails.

### Flock (R) - 25%
+10% Heads for each *other* Flock in your deck (counts the deck, not the bank). Heads +4. Two copies: 50%, three: 60%
(max copy limit 3). A set builder coin.

### Hourglass (R) - 30% - 10 gold
+30% Heads when 3 or fewer coins remain in the stack. Heads +4. Tails sends it back to the pile (extra draw), so it gets a
second chance at the end. A closer: bad early, good when the stack is almost empty.

### Contrarian (SR) - 65%
Always lands opposite to the previous flip (overrides the roll). Heads +4, Tails +1. Predictable: you know its side before
you flip. Plan it after a Tails to guarantee 4 points.

### Chain (R) - 60%
Heads: +2 points per Heads in a row, including this one. After three Heads in a row it pays 8. Needs Momentum or strong Heads odds
coins in front of it. Resets on Tails.

---

## Growing coins

### Snowball (SR) - 30% - 1 energy - 24 gold
Heads +3, Tails +1, and **+1 point on every effect for every time this coin has been flipped this run**, capped at +10. Growth is stored
on the coin instance, so it survives between levels. EV at the cap: 12 points. Plays one flip per level, so it takes
10 levels to reach the cap; in a 4-level run, think +3 at most unless you have extra draws (Lucky, Hourglass, Extra Draw chip).

### Fuse (SR) - 30% - 10 gold
Heads +1. When you *discard* it, it gains 6 charge (stored on the coin, persists between levels). On Heads it spends all of its charge as
extra points. A counter-intuitive coin: wasting a copy of Fuse is the way to power the next one. Needs two or more copies.

---

## Combo and synergy

### Lucky (N) - 30% - 10 gold
Heads: +2 points, and it returns to the draw pile and can be played again (extra draw). A "free flip" generator when
you have strong coins to feed. Capped at 3 returns per level.

### Echo (UR) - 60%
Repeats the effects the previous coin had for the same side (Heads effects after a Heads, Tails after a Tails). Echo after
Hammer on Heads is another 16 points. Echo after a coin with an empty side does nothing.

### Jester (UR) - 50% - 1 energy
Ignores its own sides: a random effect from {6 points, 4 gold, 2 energy, 2 points}. EV 3.5 points-equivalent. Safe chaos.

---

## Strength ranking

Rough order by what the simulator shows on a built 5-coin set (see `lua tools/sim.lua --coins`):

| Tier | Coins |
|---|---|
| S | Hammer (with energy), Blood, Snowball, Gambler, Dagger |
| A | Sword, Flux Capacitor, Bounty, Cursed (with odds help), Phoenix, Chain + Momentum |
| B | Spark, Focus, Lucky Seven, Contrarian, Martyr, Echo |
| C | Loaded, Copper, Bank, Miser, Vampire, Flock, Hourglass, Lucky, Jester |
| Filler | Normal |

Known issues: coin prices are flat at 15 so tier S coins are under-priced; a few economy coins (Bank, Miser) do very little
in a 4-level run because gold rarely accumulates.

## Additional coins

### Compost (N) - 47% - 10 gold

Tails: quota +2; once per level, Fortune coins gain +11% Heads for the run (max +55%).

### Square Dance (N) - 27% - 14 gold

Heads: 2 points times the square of Square Dance copies in your deck.

### Whetstone (R) - 67% - 22 gold

Heads: next 2 Steel coins gain 3 points on Heads, but add 2 quota on Tails.

### Counterfeiter (R) - 61% - 25 gold

Heads: next 2 Greed coins double all gold gained. Each Tails also loses up to 3 gold.

### Blood Pact (SR) - 58% - 32 gold

Heads: next Blood coin gains 8 points on Heads or adds 4 quota on Tails. Edge gets half of both.

### Doppelganger (SR) - 53% - 34 gold

Heads: next Chaos coin applies its resolved effects twice, including penalties.

### Good Dog (UR) - 43% - 28 gold

Heads: 2 points; return the highest-scoring coin played this level to the draw pile. Once per level.

### Conductor (R) - 73% - 24 gold

Heads: next 3 Rhythm coins gain 2 points per combo step (max 8); a broken combo adds 5 quota.
