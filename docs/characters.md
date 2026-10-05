# Characters

A character decides three things: the **default deck** (the 5 coins you start with), the **pool** (coins you may put in a
coin set from the very beginning), and the **locked list** (coins that exist for this character in the shop but are not yet
usable in sets until you buy them once). The shop sells pool and locked coins alike. Defined in
`content/characters.lua`; portraits are in `assets/characters/`.

All characters share the same rules, quotas and shop. The difference is which coins are offered to them and the
direction of the starter deck.

## The Blade - "Reliable points"

Portrait: a fox duelist with a coin and a rapier.

<!-- GEN:char-blade -->
| | |
|---|---|
| **Default deck** | Normal, Sword, Dagger |
| **Pool** | Normal, Sword, Dagger |
| **Locked** | Hammer, Blood, Vampire, Chain, Cursed, Fuse, Focus, Martyr, Snowball, Spark, Jackpot, Lifeline, Megaphone, Pot, Hot Hand, Cash Out, Doubler, Amplifier |
<!-- /GEN:char-blade -->

**Play style.** Straight points. Blade's pool is made of coins whose value does not depend on hooks: Sword (+5 on
Heads), Dagger (3.25 EV at 75%), then Hammer and Blood for burst. Energy is the constraint, because Hammer and Blood cost
energy and Blade has no energy coin in the pool; Spark must come from the Seer's or Trader's side (Blade does not
get Spark or Copper). In practice Blade alternates one heavy coin per level with free coins.

**Strengths.** The most predictable character. Easy to build a deck that clears levels 1 and 2 with a margin. Good with
Fuse (discard copies for charge) and Chain.

**Weaknesses.** Little economy: there is no gold coin in Blade's list except Vampire. Exchanging is harder to afford. The
boss quota (3.0 per coin) needs Hammer-level burst.

**A first set.** 2 Sword, 2 Dagger, 1 Normal (a set holds 5 coins); Hammer and Blood join in the shop once you have bought deck slots.

## The Seer - "Risk and changing odds"

Portrait: an owl mage with a glowing coin.

<!-- GEN:char-seer -->
| | |
|---|---|
| **Default deck** | 2 x Normal, Dagger, Focus, Spark |
| **Pool** | Normal, Dagger, Cursed, Gambler, Spark, Focus, Lucky |
| **Locked** | Horoscope, Crystal Ball, Mimic, Contrarian, Lucky Seven, Blood, Hourglass, Jester, Echo, Phoenix, Mirror, Domino, Twin, Cold Streak, Anchor, True Echo, Amplifier |
<!-- /GEN:char-seer -->

**Play style.** Gamble and manipulate. Cursed (30%, +15) and Gambler (50% of x3) are swings; Focus and Lucky Seven bend the
odds, Contrarian lets you plan the side, Echo and Jester are chaos. Spark gives energy so Gambler, Blood and Jester
can be flipped.

**Strengths.** The highest ceiling, because the combination of Cursed, Focus and the Force Heads chip can produce 15-point
flips, and Echo can duplicate them.

**Weaknesses.** Still the weakest character in the simulator (boss clear about 37% with a built set, versus about 50% Blade and 57%
Trader), but it was buffed: it now starts with a Dagger and has Horoscope, Crystal Ball and Mimic. Cursed and Gambler are not in the default deck and have to
be added from the pool.

**A first set.** 2 Spark, 1 Focus, 1 Gambler, 1 Cursed.

## The Trader - "Gold and energy"

Portrait: a merchant with a ledger and a coin.

<!-- GEN:char-trader -->
| | |
|---|---|
| **Default deck** | Normal, Loaded, Dagger |
| **Pool** | Normal, Copper, Loaded, Dagger, Sword |
| **Locked** | Spark, Bank, Miser, Hammer, Bounty, Flock, Momentum, Flux Capacitor, Cheerleader, Megaphone, Orchestra, Lifeline, Jackpot, Bettor, Anchor, Doubler, True Echo |
<!-- /GEN:char-trader -->

**Play style.** Convert gold and energy into points. Loaded (75%, +4 gold), Copper (gold or energy) and Bank fund
exchanges and shop purchases; Miser and Flux Capacitor turn gold and energy into points; Bounty converts points back into gold.
Hammer is available for burst and Trader has the energy to afford it.

**Strengths.** The strongest character in the simulator (boss clear about 75% with a built set): gold is also an extra life
because of the exchange, and the pool already contains Dagger and Sword.

**Weaknesses.** The early game is slow (no big hitters in the starting deck), and the economy coins are useless when you are poor.

**A first set.** 2 Dagger, 1 Sword, 1 Loaded, 1 Copper.

## Starting decks

Starting decks were chosen to make a plain, unbuilt start roughly comparable:

| Deck | EV per 5 flips | Quota level 1 (3 pts) |
|---|---|---|
| Blade: 4 Normal, Sword | 4.5 points | Likely |
| Seer: 2 Normal, Dagger, Focus, Spark | 1.0 + 3.25 + 2 + 1.5 = 7.75 points, plus 1 energy | Likely |
| Trader: 3 Normal, Loaded, Dagger | 1.5 + 3.25 = 4.75 points, plus 3 gold | Likely |

The default start fills all 5 starting slots; the shop sells up to 5 more slots (5 gold each).

## Unlocking characters

The Blade is available from the start. **Winning a run unlocks the next character:** a win with the Blade opens the Seer, a win with the Seer opens the Trader. Locked characters
are greyed out and cannot be started or edited.

## Collection and unlocks

- A coin is **collected** the first time it is in your deck. The Collection screen shows collected coins and a black silhouette for
  the rest.
- A locked coin is **unlocked** the first time you buy it in a shop. From then on it can go into any of that character's sets.
- A coin that is in several characters' lists (Dagger, Blood, Hammer, Spark) must be unlocked per character.
