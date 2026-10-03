# Game Ideas

Draft backlog, not an implementation plan. The goal is to make each level ask for a few memorable risk, cash-out, or build decisions while keeping the coin stack and odds easy to read.

## Best experiments to try first

### 1. Reroll one contract offer

Before accepting a contract, replace one of the three offers once. Keep the other two, and make the replacement free. This gives players a way past a bad set without turning the offer screen into repeated rerolling. A future token cost could make extra rerolls a progression sink.

### 2. Parlay the side bet

The current side bet resolves on the first flip. If it wins, offer a choice: take the payout or roll the winnings into a bet on the next coin. Limit the chain to three flips. A Tie returns the current stake and pauses the chain; a wrong call loses only the amount currently wagered. Show the odds and payout before each choice.

This adds a new decision after a win: secure the money or chase a larger payout.

### 3. Wager surplus after clearing

After the quota is met, let the player risk a chosen amount of gold on one more flip. A correct call pays extra; a wrong call loses the wager. The level payout and cleared level stay safe. This gives the post-quota phase a real gamble without making a cleared level fail again.

### 4. Reward alternating streaks

The current combo rewards repeating the same side. Add a small separate reward for a pattern such as Heads–Tails–Heads–Tails. Keep its reward modest and separate from the main multiplier so alternating and same-side streaks are both useful without multiplying each other.

## Contract ideas

Use `on_success(ctx, encounter)` and `on_failure(ctx, encounter)` for direct effects. Vary failure costs instead of making every contract reduce Heads odds.

| Contract | Completion | Success effect | Failure effect |
|---|---|---|---|
| **No Exchange** | Clear without exchanging coins | Gain 5 gold | Next exchange costs 5 more gold |
| **All In** | Clear after playing every coin still in the stack | Add one coin offer to the next shop | Start the next level with 1 less energy |
| **Comeback** | Clear after quota penalties have added at least 6 points | Gain one free exchange next level | Lose 1 coin from the deck |
| **Exact Change** | Clear with 1–2 coins left in the stack | Reduce the next shop's first coin price | Double the next shop's first reroll cost |

These are sketches: tune objective thresholds to the deck size and level quota before adding them to the offer pool.

## Longer-term build ideas

### Coin bonds

Let the player link two owned coins between levels. When the first is played, remember its result; when the second is played, a match adds a small payout and an opposite result breaks the bond. This rewards deck construction and adapting to the shuffled order. Keep the pair bonus independent of the main combo multiplier at first.

### House offers

Before a boss or endless level, choose one of two visible rules: a generous payout with a harsher penalty, or a safer level with less reward. Examples: Tails adds extra quota but the payout rises, or the House inverts every fourth flip but offers a free exchange.

### Give tokens a job

Tokens currently have no sink. Possible uses: pay one token for a second contract redraw, buy a permanent alternate coin skin, or unlock a starting coin choice. Prefer a gameplay choice over another passive stat bonus.

## Guardrails

- Show the exact stake, odds, and possible loss before a wager.
- Keep a failed contract's penalty bounded; avoid removing the last coin from a deck.
- Prefer effects that change what the player chooses over another flat probability penalty.
- Prototype one idea at a time. Use simulation for economy changes and playtests for whether the choice feels tense.
