# Draw system rework plan

**Status:** design draft. No draw rules changed.

## Current problem

The game draws up to five coins into an opening hand. Keeping them places all five into a queue, but the bank displays only the first three. Two coins can therefore be fixed in order while hidden and excluded from the displayed pile count. Opening discards remove coins for the entire level rather than replacing them. During play, only the first visible coin can be flipped.

## Option A: three selectable coins

Keep a hidden shuffled pile. Show up to three coins and let the player choose one to flip. Refill that slot after the flip. This keeps some draw uncertainty while enabling short combinations. Ordinary discard can remain limited to the front slot, preserving Crystal Ball's ability to discard any visible coin. Correct the opening hand so only three kept coins enter the visible bank; place any others in the counted hidden pile.

## Option B: whole deck visible (current recommendation to prototype)

Show every owned coin, up to ten, in a compact bank. Select any unplayed coin to flip. Move it to a played area until a named return effect or Exchange brings it back. Remove the shuffled draw pile and the five-coin opening mulligan. Random flips, shop offers, and level modifiers remain uncertain, while the player controls coin order and can build planned combinations.

This increases power considerably. Focus can reliably precede Hammer, Spark can pay for an expensive coin, and engine coins can activate before their matching tags. The existing quota and proposed upgrades need paired simulations before final balance values are chosen.

Full selection also changes discard and draw effects. Keep a way to discard a coin for Fuse and low-energy situations. Decide its cost or limit in the prototype. Crystal Ball's current ability to discard any visible coin becomes redundant and needs a new effect. Lucky and other extra-draw effects should return a played coin to the available bank. Exchange should return its selected played coins to that bank.

## Implementation sequence

1. Build a prototype with distinct available, resolving, played, and discarded coin zones. Each coin instance belongs to exactly one zone.
2. Show up to ten coins in a two-column, five-row bank. Use compact icons and odds; show full text for the selected coin on the main stage. Mouse, keyboard, and controller must select the same instance.
3. Make “next coin” buffs apply to the next selected flip. Selection alone must not consume a buff or RNG roll.
4. Resolve discard, Crystal Ball, Lucky, Exchange, and saved-run migration. Existing saved runs can contain the old queue or opening hand; never silently reshuffle one.
5. Compare fixed order, three-choice, and full-bank play on the same spread-out seeds. Update simulator bots to choose coin order. Measure setup combinations, discards, exchanges, level clears, and wins before changing quota.

Choose the version that makes sequencing fun without removing the risk of flips. If full-bank play becomes too controlled, use three selectable coins with a hidden pile.
