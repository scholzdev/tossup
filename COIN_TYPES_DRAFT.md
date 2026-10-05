# Selective coin types — design draft

**Status:** proposal only. No coin types or effects below are implemented.

## Core idea

Most coins have **no type**. A few coins carry one type so other coins can reward a deliberate deck composition. Playing an engine coin grants a buff to a type; eligible coins receive it automatically when they are flipped. They can be in the hidden draw pile when the buff is granted. Nothing needs to be placed, held, or manually activated.

Run-wide buffs survive into later levels, so drawing an engine after its payoff coin is still useful. Earlier flips are never changed. A newly bought coin with the matching type receives existing buffs. Show its type and current bonuses on the coin tooltip and flip preview.

## First three types

These are **target tags**, not a classification of every coin. Each listed coin has exactly one type. Everything not listed stays untyped, including Normal, Copper, Spark, Megaphone, Cheerleader, Amplifier, Echo, True Echo, and Crystal Ball.

| Type | Tagged coins | Why these coins belong |
|---|---|---|
| **Steel** | Sword, Dagger, Hammer, Chain, Jackpot | Their main payoff is direct points. This is the high-score target. |
| **Wager** | Blood, Vampire, Gambler, Martyr, Lucky Seven | Their strong result comes with low odds, a quota risk, or another gamble. |
| **Fortune** | Lucky, Loaded, Bank, Focus, Horoscope | They improve odds, return for another try, or fund later plays. |

An untyped coin still has its normal effects and can be an engine that buffs a type. This gives support coins a job without letting them automatically buff themselves.

## Example effects to prototype

The amounts are starting points for playtests. These effects would replace or extend the named coin's current text; they do not describe current gameplay.

| Source coin | Draft trigger | Type buff | Duration |
|---|---|---|---|
| **Spark** (existing, untyped) | Heads | Every Steel coin gains **+1 base point** on its scoring side. Spark still gives 2 energy. | Rest of run; stacks. |
| **Whetstone** (new, untyped) | Heads | Every Steel coin gains **+2 base points** on its scoring side. | Rest of run; stacks. |
| **Megaphone** (existing, untyped) | Heads | The next **two Steel flips** score **×2**. Other flips do not spend the charges. | Charges carry across levels. |
| **Crooked Die** (new, untyped) | Any result | Wager coins gain **+7 percentage points** chance to double their numeric outcome. This also doubles quota penalties on a bad result. | Rest of run; stacks. |
| **Cheerleader** (existing, untyped) | Heads | Fortune coins gain **+7 percentage points Heads**. | Rest of run; stacks. |

An engine grants its run-wide buff **once per coin instance per level**. Returning or exchanging that instance can score again but cannot farm another permanent upgrade in the same level. Type buffs add to the printed coin effect before the existing combo and chip multipliers. Extra Heads chance comes from Tails, leaving any Edge chance intact.

## What a run could do

### Steel score engine

Build around Spark, Whetstone, Megaphone, and a Steel finisher such as Jackpot. Spark and Whetstone can appear anywhere in the draw: when played, they upgrade every future Steel flip. If Jackpot was already played this level, it benefits next level. Megaphone's charges wait for Steel rather than disappearing on an unrelated coin.

After two Whetstone upgrades and one Spark upgrade, Jackpot's Heads starts at **25 + 4 + 1 = 30 points**. With a Megaphone charge, a ×3 Heads combo, and Double Down, that flip scores **30 × 2 × 3 × 2 = 360 points**. It still needs Jackpot to land Heads and the combo to survive. Baton could raise the combo multiplier further.

### Wager deck with a painful downside

Crooked Die makes Blood, Gambler, and Martyr attractive together. A doubled Blood Heads scores **20** instead of 10; a doubled Tails raises quota by **12** instead of 6. Its current Edge scores 5 and raises quota by 3; doubling makes that **10 points and quota +6**. The same engine that enables an absurd hit can make a bad run worse.

### Fortune support deck

Cheerleader upgrades Fortune's Heads odds across levels. Lucky gets more chances to return, Loaded pays gold more often, and Focus can more reliably give the *next coin* its large one-flip odds boost. Fortune supports the expensive Steel or Wager payoff without making every coin in the deck part of the type system.

## Boundaries to test

- Keep the current shuffled pile, opening hand, and three-coin preview. Type buffs provide deckbuilding synergy without selecting or arranging the next coin.
- Start with only these 15 tagged coins. Do not tag a coin merely to fill a category.
- Show accumulated type buffs clearly, including the number of Megaphone charges and Crooked Die's risk, before a flip.
- Test how often the engine coins earn their deck slot, how large scores get, and whether late run quotas need adjustment. Do not raise quotas before the engine deck actually proves stronger.
- Check whether repeated run-wide Steel bonuses need a cap. Prefer a visible cap if testing shows runaway scores are common rather than rare highlights.

## RACCOIN inspiration: stranger flip coins

The [RACCOIN coin list](https://raccoin.wiki.gg/wiki/Coins) has 150 entries. Its coins often touch, hunt, or sit beside each other in a physical cabinet. These are **new ideas adapted for a shuffled flip deck**, not existing RACCOIN effects or implemented game content. Only the stated target coins need one of the three types above; the proposed source coins can stay untyped.

| RACCOIN inspiration | Draft coin for this game | Example flip effect and joke |
|---|---|---|
| Chummy Coin buffs other Chummy Coins | **Clique** (Common) | Heads: 2 points, then every Clique copy gains +2 base points for the run. Three friends become unbearable together; one friend is merely okay. No type tag needed. |
| Poocoin fertilizes plants, then disappears | **Compost** (Common, untyped) | Tails: quota +2, then every **Fortune** coin gains +11 percentage points Heads for the run. A bad flip literally fertilizes your luck. |
| Eggoin becomes Hen Coin; Hen breeds eggs | **Suspicious Egg** (Common, untyped) | First level: almost useless. Next level it becomes **Hen**. Hen Heads adds one Egg to your deck. The chicken empire raises your deck-size quota as it grows. |
| Creditoin's value rises with Debt, but scoring costs money | **Debt Goblin** (Rare, Wager) | Buying it gives 25 gold and 5 Debt. Debt grows by 5 each level. Heads scores twice your Debt; Tails pays Debt in gold, and unpaid debt raises quota. Terrible financial advice that sometimes wins. |
| Jetoin can be worth -100 to +200 | **Mood Swing** (Rare, Wager) | Heads: +80 points. Tails: quota +40. Edge: +20 points and quota +20. A coin whose average mood is a legal dispute. |
| Square Coin scales with the square of its copy count | **Square Dance** (Common, untyped) | Heads scores `2 × (Square Dance copies in deck)²`. One copy gives 2; three copies give 18 each. The Common copy limit makes the joke strong but finite. |
| Dogoin fetches a valuable scored coin | **Good Dog** (Epic, untyped) | Heads: return the highest-scoring coin already flipped this level to the draw pile, once per level. It can fetch Jackpot; it cannot fetch itself. |
| Factorial Coin pays from exchange count | **Accountant** (Rare, untyped) | Heads scores `4 × (exchanges used this level + 1)!`: 4, 8, 24, then 96 at the normal three-exchange limit. You pay gold and replay coins to prepare one ridiculous audit. |
| 1/2 Coin consumes half your money for value | **Half Now** (Uncommon, untyped) | Heads: spend half your gold and score three times the gold spent. A spectacular clear can leave you unable to buy the engine you wanted. |

**First prototypes worth trying:** Clique for a simple copy engine, Debt Goblin for funny risk, and Good Dog for a dramatic replay. Compost is the first direct example of an untyped source upgrading a typed target without any placement mechanic.
