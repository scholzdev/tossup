# Edge, synergy, and quota draft

**Status:** the Edge, engine-coin, and quota proposals below are unimplemented.

The old repeatable shop purchase of +3 percentage points Heads has been removed. Odds growth in this proposal comes from engine coins with a deck-slot cost, timing, and a run-wide cap.

## Goal

A strong deck should come from combining coins, with occasional spectacular wins and painful losses. Edge should be a meaningful third outcome on selected coins. The extra power needs higher late-run quotas, while a starting deck still has a fair first level.

## Core rules to try

- Give only selected coins an Edge chance. Their listed Heads chance stays as proposed; Edge takes probability from Tails. Show all three chances on the coin and in the flip preview.
- Give each selected coin a written Edge effect. Blood keeps its current half-Heads, half-Tails behavior. Other Edge effects can use energy, draws, or buffs rather than trying to halve those effects.
- Edge is its own result: it neither extends a Heads streak nor counts as Tails for Martyr. A double-outcome effect doubles the numeric parts of Edge just as it does Heads or Tails.
- Use a few **tags** to connect several coins. A coin only needs a tag when a payoff uses it; this is not a demand to classify every coin. Start with Steel (Sword, Dagger, Hammer, Anchor, Chain), Wager (Blood, Vampire, Martyr, Cursed, Gambler, Lucky Seven), Fortune (Lucky, Loaded, Bank, Bounty, Jackpot), and Charge (Spark, Focus, Capacitor, Fuse). Tag membership is draft, especially for coins shared by two themes.

## First Edge coins

Percentages are Heads / Edge / Tails. Heads values use the proposed odd-numbered odds list. These are candidates, not final balance values.

| Coin | Chances | Edge effect | Why it belongs |
|---|---:|---|---|
| Blood | 37 / 13 / 50 | Score 5; raise quota by 3 | A real gain with a real cost. Its current Edge chance is 9%; 13% is the first visibility test. |
| Hammer | 11 / 7 / 82 | Score 5 | Makes a costly Hammer flip less binary without replacing its 16-point Heads. |
| Spark | 73 / 11 / 16 | Gain 1 energy and score 1 | Fuels expensive coins while giving a small immediate reward. |
| Focus | 67 / 9 / 24 | Score 2; next coin gets +15 percentage points Heads | Sets up the next flip at half the strength of its Heads setup. |
| Lucky | 19 / 17 / 64 | Return to the pile, score 0 | Another chance to hit its strong Heads; still subject to the existing return cap. |

Start by testing Blood, Hammer, and Focus. Add Spark and Lucky only if Edge is readable in play and does not flood the pile with repeat flips.

## Upgrade coins

**Recommendation:** try in-deck engine coins first. They take a deck slot, appear in the draw, and raise quota through the existing deck-size rule. A free permanent shop buff would need its own price and power budget. The engine coins are untagged, so they do not boost themselves. Their effects activate when the coin resolves, affect later eligible flips, and persist for the rest of the run. Previously resolved flips never change.

| New coin | Draft rarity and chances | Own flip | Upgrade granted |
|---|---|---|---|
| Whetstone | Uncommon; 61 / 11 / 28 | Heads: 2 points. Edge: 1 point. Tails: quota +2. | On Heads or Edge, future **Steel** flips gain **+5 percentage points Heads** for the run. |
| Crooked Die | Rare; 43 / 13 / 44 | Heads: 2 points. Edge: 1 point and quota +1. Tails: quota +3. | On any result, future **Wager** flips gain **+5 percentage points chance to double their outcome** for the run. The Tails activation comes at a painful immediate cost. |

Rules for both upgrades:

1. An engine can grant its upgrade at most once per level, including when it returns to the pile. A second copy can still be flipped, but a tag gains at most one upgrade of each kind per level.
2. Heads upgrades for a tag stack to **+15 percentage points** across a run. Double-outcome chance stacks to **15%**. Show current bonuses beside the affected coin's odds.
3. Extra Heads chance comes out of Tails first; Edge chance stays fixed. Never let the three chances exceed 100%.
4. The double check happens after the side is known. If it succeeds, double that coin's numeric score, gold, and quota-penalty effects, including both numeric parts of an Edge. Example: doubled Blood Edge scores 10 and raises quota by 6; doubled Blood Tails raises quota by 12. Energy, shields, draws, and new upgrades happen once. Doubling cannot trigger itself again.
5. A buff applies only to coins with the target tag, including a different copy of the same named coin. Replaying the engine coin cannot bypass the per-level cap.

The 5-point bonuses may be too small for a card that costs a deck slot. Test whether either engine gets bought and kept before raising the numbers. If players ignore it, try +7 percentage points per activation or a stronger immediate flip effect. Do not raise quotas to pay for upgrades nobody uses.

## Combinations worth drafting

- **Whetstone + Focus + Hammer:** Whetstone raises Steel Heads across levels; Focus can give Hammer one large short-term boost. Spark pays Hammer's energy cost. Hammer still has a large Tails region.
- **Crooked Die + Blood + Martyr:** Doubled Blood Heads pays 20 points, doubled Tails adds 12 quota, and Edge doubles both sides. Martyr converts earlier Tails into a later recovery. This is the volatile deck.
- **Anchor + Chain:** Anchor protects a Heads streak; Chain pays for keeping it. A future Steel Edge payoff could care about a shield being spent, but start with the existing interaction.
- **Loaded + Bank:** Gold generation buys more upgrades or exchanges. Keep the gold route separate from Wager's double-outcome buff so one engine cannot multiply both points and income without paying deck slots for both tags.

An optional later payoff: Vampire gains +2 points on Heads if a Wager coin raised the quota earlier this level. This gives Blood and Martyr a partner even when their risky flip went badly. Count the event once per level, not once per quota point.

## Three visible coins: make the preview a choice

Currently the bank shows three coins, but the front one is the only coin that can be flipped or normally discarded. The other two are a forecast. That gives useful information for chips and for a few next-coin effects, yet it gives little control over when a setup coin meets its payoff. With tags and upgrade coins, this risks making combinations feel like lucky draw order rather than a deckbuilding decision.

**Prototype:** show three face-up coins and let the player flip **one of the three**. The other two stay; refill the empty place from the shuffled pile after the flip. Every owned coin still appears once per level unless an explicit return effect says otherwise. A buff that says “next coin” applies to the next coin actually chosen and flipped. Show the updated odds on all three cards before the choice.

Keep the ordinary free discard limited to the front slot, clearly marked as such. Crystal Ball still has a distinct purpose because it can discard *any* visible coin. The opening five-coin mulligan can stay for the first prototype, but measure whether it plus the three-way choice removes too much uncertainty.

This is a substantial power increase: Focus can be followed by Hammer on purpose, Spark can feed a costly coin, and Whetstone can be played before the Steel cards already in view. First compare the current fixed-order bank with the three-way choice using the same decks and seeds. If unrestricted choice makes runs too easy, test **one off-front play per level** before increasing every quota again. Preserve the random pile and risky flip odds in either version.

## Quota proposal

The current base quota is `round(coins in deck × quota per coin)`, before stake multipliers and modifiers. Bigger decks already have a higher target. Keep the opening level unchanged; engine coins arrive through later shops and need time to stack.

| Level | Current per coin | First candidate with engines | Example with 5 coins: current → candidate |
|---|---:|---:|---:|
| Opening | 0.7 | 0.7 | 4 → 4 |
| Second Chance | 1.4 | 1.6 | 7 → 8 |
| High Stakes | 2.5 | 2.9 | 13 → 15 |
| The House | 4.5 | 5.2 | 23 → 26 |

This is a **candidate**, not an automatic increase. In the paired simulation of the proposed 43 Heads values, a greedy five-coin deck with Blood and its current 9% Edge chance won only 0.2% of 1,000 runs. That bot is crude, but the result argues against raising quotas before the upgrade coins demonstrably improve real decks. The suggested route should be tested against plain starter decks, a strong built deck, and the Wager/Steel combinations at several stakes, with both fixed-order and selectable banks. If ordinary decks collapse, reduce the proposed increase or put some of the difficulty in later stakes rather than the base route.

Balance targets for the first prototype: engine decks should outperform the same deck without an engine often enough to justify its slot; a bad Crooked Die sequence should sometimes lose a level; and a good sequence should sometimes rescue a run. Track purchase rate, upgrade activations, Edge and double counts, quota reached by level, and wins using paired seeds. Ten runs are useful for spotting feel and bugs; use at least several hundred paired seeds for quota decisions.

## Decisions to make after a playable prototype

- Whether Blood's Edge chance should stay at 9% or move to 13–17% for visibility.
- Whether Crooked Die's 5-point double chance is exciting enough once it is paid for with a deck slot.
- Which tag memberships create real choices in character pools and rarity-limited starting sets.
- Whether a shop upgrade should eventually exist alongside engine coins. If added, price and cap it separately and keep its effect visible on the run screen.
