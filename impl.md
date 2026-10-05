Yep — I'd make this one **implementation-focused**, so an agent can take the design doc plus this file and start building without having to infer the architecture.

 # Coin Roguelike — Implementation Specification

 ## 1\. Technology

 Use:

 - **Lua 5.1-compatible Lua**
- **LÖVE 2D**
- No external game engine.
- No ECS framework unless there is a compelling reason.
- Keep dependencies minimal.

 The game should be a 2D desktop game.

 The first milestone is a functional ugly prototype, not a polished commercial game.

---

 # 2\. Primary Goal

 Build a playable prototype demonstrating the core loop:

```
Start Run
    ↓
Encounter
    ↓
Flip Coins
    ↓
Resolve Effects
    ↓
Complete Objective
    ↓
Choose Reward
    ↓
Next Encounter
    ↓
Repeat
```

 The prototype should establish whether the **probability manipulation + roguelike progression** loop is fun.

 Do not spend significant time on:

 - Art
- Narrative
- Complex animations
- Meta-progression
- Audio
- Multiple worlds
- Localization
- Save systems

 until the core loop works.

---

 # 3\. Architecture

 Separate the project into three conceptual layers.

```
┌──────────────────────────────┐
│          Presentation        │
│          LÖVE 2D             │
│                              │
│ UI / Rendering / Input       │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│          Game State           │
│                              │
│ Run / Encounter / Player     │
│ Coins / Relics / Resources   │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│       Simulation / Rules      │
│                              │
│ RNG / Probability / Effects  │
│ Coin resolution / Modifiers  │
└──────────────────────────────┘
```

 The simulation layer should have as few LÖVE dependencies as possible.

 Ideally, the majority of the game rules can be tested without opening a window.

---

 # 4\. Directory Structure

 Use this structure:

```
coin-roguelike/
│
├── main.lua
├── conf.lua
├── README.md
├── DESIGN.md
├── impl.md
│
├── src/
│   ├── game.lua
│   ├── run.lua
│   ├── player.lua
│   ├── encounter.lua
│   ├── reward.lua
│   │
│   ├── coins/
│   │   ├── coin.lua
│   │   ├── coin_factory.lua
│   │   └── coin_effects.lua
│   │
│   ├── probability/
│   │   ├── rng.lua
│   │   ├── probability.lua
│   │   └── modifiers.lua
│   │
│   ├── relics/
│   │   └── relic.lua
│   │
│   └── ui/
│       ├── button.lua
│       ├── coin_view.lua
│       ├── encounter_view.lua
│       ├── reward_view.lua
│       └── text.lua
│
├── content/
│   ├── coins.lua
│   ├── relics.lua
│   └── encounters.lua
│
└── tests/
    ├── test_rng.lua
    ├── test_probability.lua
    ├── test_coin.lua
    ├── test_effects.lua
    └── test_encounter.lua
```

---

 # 5\. Game State

 The complete run should be represented by a serializable Lua table.

 Example:

```
run = {
    seed = 123456,

    player = {
        hp = 100,
        max_hp = 100,
        gold = 50,
        energy = 3,
    },

    coins = {},

    relics = {},

    current_node = nil,

    encounter_index = 1,
}
```

 Avoid storing important state only inside UI objects.

 The UI should display state, not own it.

---

 # 6\. Random Number Generator

 Do NOT scatter `math.random()` throughout the project.

 Create one RNG abstraction.

 Example API:

```
local RNG = {}

function RNG.new(seed)
    -- initialize deterministic state
end

function RNG:random()
    -- return [0, 1)
end

function RNG:random_int(min, max)
end

function RNG:chance(probability)
    -- probability in [0, 1]
end

function RNG:choose(list)
end

return RNG
```

 Every random event in the game should go through this RNG.

---

 # 7\. Deterministic Seeds

 Every run has a seed.

 Example:

```
run.seed = 381927
```

 The seed should determine all random events in the run.

 This is important for:

 - Debugging
- Reproducing bugs
- Automated testing
- Sharing runs
- Future daily challenges
- Agent development

 A bug report should eventually be reproducible with:

```
Seed: 381927
Encounter: 7
```

---

 # 8\. Probability Representation

 Represent probability as a number between `0` and `1`.

 Example:

```
coin.heads_probability = 0.65
```

 Tails is derived:

```
tails_probability = 1 - heads_probability
```

 Do not store both independently.

 This prevents invalid states such as:

```
Heads = 70%
Tails = 60%
```

---

 # 9\. Probability Modifiers

 Probability modifications should be represented explicitly.

 Example:

```
modifier = {
    type = "add",
    amount = 0.10,
}
```

 A coin might have:

```
coin.base_heads_probability = 0.50

coin.modifiers = {
    ...
}
```

 Eventually support modifier types such as:

```
add
multiply
set
invert
clamp
conditional
```

 Do not over-engineer this in the first prototype.

 Start with:

```
add
set
```

---

 # 10\. Coin

 A coin is a data object plus behavior.

 Minimum representation:

```
coin = {
    id = "copper_coin",

    name = "Copper Coin",

    heads_probability = 0.50,

    heads_effects = {},
    tails_effects = {},

    modifiers = {},
}
```

 A coin should NOT know about rendering.

 It should not contain:

```
coin.x
coin.y
coin.sprite
coin.animation
```

 Those belong to the UI/presentation layer.

---

 # 11\. Coin Flip

 The basic operation:

```
result = coin:flip(rng)
```

 Returns:

```
"Heads"
```

 or:

```
"Tails"
```

 Example:

```
function Coin:flip(rng)
    if rng:chance(self:get_heads_probability()) then
        return "Heads"
    end

    return "Tails"
end
```

 Do not resolve effects inside `flip()`.

 Flipping and resolving should be separate operations.

---

 # 12\. Coin Resolution

 After flipping:

```
result = coin:flip(rng)

effects = coin:get_effects(result)

game:apply_effects(effects)
```

 This separation is important.

 It allows:

 - Rerolls
- Result manipulation
- Copying results
- Prediction
- Result previews
- Delayed effects
- Replay systems

---

 # 13\. Effects

 Effects should be data-driven.

 Example:

```
{
    type = "gold",
    amount = 5
}
```

 Other initial effects:

```
gold
damage
heal
energy
lose_hp
multiplier
extra_flip
reroll
force_result
modify_probability
```

 Example:

```
{
    type = "damage",
    amount = 5
}
```

 Avoid making every effect a custom Lua function initially.

 Use a central effect resolver:

```
Effects.apply(game_state, effect)
```

---

 # 14\. Effect Resolver

 Example:

```
function Effects.apply(state, effect)

    if effect.type == "gold" then
        state.player.gold =
            state.player.gold + effect.amount

    elseif effect.type == "damage" then
        state.encounter.enemy.hp =
            state.encounter.enemy.hp - effect.amount

    elseif effect.type == "energy" then
        state.player.energy =
            state.player.energy + effect.amount

    end
end
```

 Later this can become more sophisticated.

 For the prototype, keep it explicit and easy to debug.

---

 # 15\. Effect Context

 Effects will eventually need access to context.

 Example:

```
context = {
    state = game_state,
    coin = coin,
    result = "Heads",
    flip_number = 4,
    heads_streak = 3,
}
```

 This allows effects to eventually support:

```
+1 Gold per current Heads streak
Deal damage equal to multiplier
If this is the third Heads, duplicate
```

 Do not implement every contextual effect immediately.

---

 # 16\. Flip Pipeline

 A complete flip should follow this sequence:

```
1. Determine coin order
2. Calculate current probability
3. Generate RNG result
4. Produce raw result
5. Apply global result modifiers
6. Record result
7. Update streak/state
8. Resolve coin effects
9. Trigger relics
10. Trigger encounter effects
11. Check objective
12. Check enemy state
13. Update UI
```

 This pipeline should eventually be centralized.

 Do not let individual UI components perform random game logic.

---

 # 17\. Flip Result Object

 Instead of returning only `"Heads"`, eventually return a result object.

 Example:

```
result = {
    coin_id = "copper_coin",

    raw_result = "Heads",

    final_result = "Heads",

    heads_probability = 0.65,

    flip_index = 4,

    effects = {},
}
```

 This will make debugging and animation much easier.

---

 # 18\. Player

 Minimum player state:

```
player = {
    hp = 100,
    max_hp = 100,

    gold = 50,

    energy = 3,
    max_energy = 3,
}
```

 Later:

```
multiplier
luck
prediction bonuses
temporary buffs
```

 Do not add these until required.

---

 # 19\. Coin Inventory

 The player owns a list of coins.

 Example:

```
run.coins = {
    coin1,
    coin2,
    coin3,
    coin4,
    coin5,
}
```

 Start with a maximum of 5 active coins.

 The player flips all active coins during an encounter unless an effect says otherwise.

---

 # 20\. Initial Coins

 Implement these first.

 ## Copper Coin

```
50% Heads

Heads:
+2 Gold

Tails:
+1 Energy
```

 ## Sword Coin

```
50% Heads

Heads:
+5 Damage

Tails:
Nothing
```

 ## Lucky Coin

```
50% Heads

Heads:
+1 Extra Flip

Tails:
Nothing
```

 ## Cursed Coin

```
25% Heads

Heads:
+15 Damage

Tails:
Lose 2 HP
```

 ## Loaded Coin

```
75% Heads

Heads:
+4 Gold

Tails:
Nothing
```

 Five coins are enough for the first prototype.

---

 # 21\. Encounter

 An encounter contains:

```
encounter = {
    type = "combat",

    turns_remaining = 6,

    objective = {
        type = "damage",
        target = 30,
        current = 0,
    },

    enemy = {
        hp = 30,
        max_hp = 30,
    },
}
```

---

 # 22\. Initial Encounter

 Only implement one encounter type initially:

 ## Damage Challenge

 Goal:

```
Deal 30 damage before 6 flips.
```

 The encounter ends when:

```
enemy.hp <= 0
```

 or:

```
turns_remaining <= 0
```

 Victory:

```
enemy.hp <= 0
```

 Defeat:

```
turns_remaining <= 0
AND enemy.hp > 0
```

---

 # 23\. Encounter Turn

 One turn:

```
1. Reset temporary state if appropriate.
2. Player presses FLIP.
3. Coins are flipped.
4. Results are recorded.
5. Effects resolve.
6. Relics trigger.
7. Enemy state updates.
8. Objective updates.
9. Turn counter decreases.
10. Check victory/defeat.
```

---

 # 24\. Initial UI

 Do not create elaborate menus.

 Use a simple layout.

```
+------------------------------------------------+
| HP: 100/100      GOLD: 50       ENERGY: 3     |
+------------------------------------------------+
|                                                |
|                 ENEMY                          |
|              HP: 30 / 30                       |
|                                                |
+------------------------------------------------+
|                                                |
|   [ COIN ]  [ COIN ]  [ COIN ]  [ COIN ]      |
|                                                |
|   50%       50%       75%       25%            |
|                                                |
+------------------------------------------------+
|                                                |
|               [ FLIP ]                         |
|                                                |
+------------------------------------------------+
|                                                |
| Last Results:                                  |
| Heads  Heads  Tails  Heads                     |
|                                                |
+------------------------------------------------+
```

 Use placeholder rectangles and text.

 No sprites required.

---

 # 25\. Coin Animation

 After the core simulation works, add:

```
Coin idle
→ Player clicks FLIP
→ Coin rotates/flips
→ Result displayed
→ Effects animate
```

 The animation must not control the simulation.

 The simulation resolves immediately.

 The UI can animate the already-determined result.

 This avoids gameplay depending on animation timing.

---

 # 26\. Rewards

 After winning an encounter, show 3 choices.

 Example:

```
Choose a reward:

[ Loaded Coin ]
75% Heads
Heads → +4 Gold

[ Sword Coin ]
50% Heads
Heads → +5 Damage

[ Lucky Coin ]
50% Heads
Heads → Extra Flip
```

 Selecting a reward adds the coin to the player's collection.

 For the first prototype, rewards should only be coins.

---

 # 27\. Shop

 After several encounters, show a simple shop.

 Initial shop functionality:

```
Buy Coin
Remove Coin
Leave
```

 Do not implement a complex shop economy initially.

---

 # 28\. Run Progression

 The first prototype can use a simple linear structure:

```
Encounter
→ Reward
→ Encounter
→ Reward
→ Shop
→ Encounter
→ Reward
→ Boss
```

 Do not build the branching map until the core loop works.

---

 # 29\. Boss

 First boss:

```
Name:
The House

HP:
100
```

 Special mechanic:

```
Every 5th flip:
Invert all coin results.
```

 Meaning:

```
Heads → Tails
Tails → Heads
```

 The purpose is to demonstrate that encounters can modify the fundamental coin rules.

---

 # 30\. Relics

 Do not implement relics in the first playable prototype.

 After the basic game works, implement:

 ## Magnet

```
Every 3 consecutive Heads:
+5% Heads probability for the current encounter.
```

 ## Lucky Penny

```
The first Tails each encounter becomes Heads.
```

 ## Broken Clock

```
Every 10th flip is guaranteed Heads.
```

 Relics should be global run modifiers.

---

 # 31\. Streak Tracking

 Track:

```
run.current_streak = {
    result = nil,
    count = 0,
}
```

 When a result occurs:

```
same result:
count += 1

different result:
result = new result
count = 1
```

 Eventually expose:

```
state.heads_streak
state.tails_streak
```

 as convenient values.

---

 # 32\. Prediction System

 Do not implement initially.

 When implemented, the player should be able to choose:

```
[ HEADS ] [ TAILS ]
```

 before the flip.

 The prediction should be part of the simulation state.

 Example:

```
encounter.prediction = "Heads"
```

 After the flip:

```
correct:
apply prediction rewards

incorrect:
apply prediction penalties
```

---

 # 33\. Data-Driven Content

 Coins should eventually be defined in `content/coins.lua`.

 Example:

```
return {

    copper_coin = {
        name = "Copper Coin",
        heads_probability = 0.50,

        heads = {
            {
                type = "gold",
                amount = 2,
            }
        },

        tails = {
            {
                type = "energy",
                amount = 1,
            }
        }
    },

}
```

 This makes adding content easy.

 A new coin should ideally require no modification to core engine code.

---

 # 34\. Content IDs

 Every content object needs a stable ID.

 Example:

```
copper_coin
sword_coin
lucky_coin
cursed_coin
loaded_coin
```

 Never rely on display names as IDs.

 Display names can change.

 IDs should not.

---

 # 35\. Game State Machine

 The game should have explicit states.

 Minimum:

```
MENU
ENCOUNTER
REWARD
SHOP
GAME_OVER
VICTORY
```

 Example:

```
game.state = "ENCOUNTER"
```

 Transitions:

```
MENU
 ↓
ENCOUNTER
 ↓
REWARD
 ↓
ENCOUNTER
 ↓
SHOP
 ↓
ENCOUNTER
 ↓
BOSS
 ↓
VICTORY
```

 The UI displayed should depend on the game state.

---

 # 36\. Input

 For the prototype, support:

 - Mouse
- Keyboard

 Mouse:

```
Click FLIP
Click coin
Click reward
```

 Keyboard:

```
SPACE → Flip
ESC → Back / Menu
1/2/3 → Reward selection
```

---

 # 37\. Testing

 The simulation should be testable without LÖVE.

 At minimum, test:

 ## RNG

```
Same seed → same sequence.
Different seeds → potentially different sequences.
```

 ## Probability

```
50% coin produces approximately 50% Heads over many trials.
```

 Do not expect exact statistical equality.

 ## Coin

```
Coin produces valid results.
```

 ## Effects

```
Gold effect increases gold.
Damage effect decreases enemy HP.
Energy effect increases energy.
```

 ## Encounter

```
Killing enemy produces victory.
Running out of turns produces defeat.
```

 ## Determinism

```
Same seed + same actions
=
same results.
```

---

 # 38\. Statistical Tests

 For probability testing, use large sample sizes.

 Example:

```
Run 100,000 flips of a 50/50 coin.

Expected:
~50,000 Heads
~50,000 Tails
```

 Do not write tests requiring exactly:

```
50,000 Heads
50,000 Tails
```

 Randomness does not work that way.

 Use a reasonable tolerance.

---

 # 39\. Debug Mode

 Implement a debug overlay.

 Toggle with:

```
F3
```

 Show:

```
Seed: 381927
Game State: ENCOUNTER
Encounter: 3
Turn: 4 / 6

Player:
HP: 100
Gold: 42
Energy: 2

Coins:
Copper
Sword
Loaded

Last RNG:
0.72193

Last Results:
Heads
Tails
Heads
```

 This is particularly useful for development with coding agents.

---

 # 40\. Event Log

 Maintain a simple event log.

 Example:

```
> Flipping 3 coins...

Copper Coin → HEADS
+2 Gold

Sword Coin → TAILS
Nothing

Loaded Coin → HEADS
+4 Gold

Total:
+6 Gold
```

 The event log is both useful for debugging and eventually useful as player-facing feedback.

---

 # 41\. No Hidden Randomness

 Every random action should be traceable.

 Bad:

```
math.random()
```

 inside arbitrary gameplay code.

 Good:

```
local roll = state.rng:random()
```

 All random operations should be reproducible from the run seed.

---

 # 42\. Animation Architecture

 Use a simple event queue.

 Simulation generates:

```
{
    type = "coin_flipped",
    coin_id = "copper_coin",
    result = "Heads",
}
```

 Then the UI consumes the event.

 Example:

```
Simulation
    ↓
Event
    ↓
Animation Queue
    ↓
Visual Effect
```

 Never make the simulation wait for animations.

---

 # 43\. Audio

 Ignore audio during the first prototype.

 Later add:

```
coin flip
heads
tails
damage
gold
reward
victory
defeat
```

---

 # 44\. Art Direction

 Initial prototype:

```
Background: dark gray
Cards/coins: simple circles
Text: readable system font
Buttons: rectangles
Effects: colored text
```

 Do not create custom art yet.

 The prototype should look intentionally utilitarian.

---

 # 45\. Development Phases

 ## Phase 1 — Simulation

 Implement:

 - RNG
- Player
- Coin
- Effects
- Encounter
- Win/loss
- Seeded runs

 No polished UI.

 ### Completion criterion

 A Lua test can simulate an entire encounter.

---

 ## Phase 2 — Basic UI

 Implement:

 - Coin display
- Flip button
- Enemy HP
- Player stats
- Result log

 ### Completion criterion

 A human can play an encounter.

---

 ## Phase 3 — Roguelike Loop

 Implement:

 - Rewards
- Multiple encounters
- Coin acquisition
- Shop
- Run state
- Game over

 ### Completion criterion

 A complete run can be played.

---

 ## Phase 4 — Build Mechanics

 Implement:

 - Probability modifiers
- Streaks
- Rerolls
- Forced outcomes
- Relics
- Conditional effects

 ### Completion criterion

 Different builds produce noticeably different strategies.

---

 ## Phase 5 — Bosses

 Implement:

 - The House
- Result inversion
- Boss-specific rules

 ### Completion criterion

 The game can meaningfully challenge a developed build.

---

 ## Phase 6 — Juice

 Only now add:

 - Coin animations
- Screen shake
- Particles
- Sound
- Better typography
- Transitions
- Visual feedback
- Music

---

 # 46\. Agent Development Rules

 When using an AI coding agent:

 1. Do not rewrite the architecture unnecessarily.
2. Do not introduce a framework without asking.
3. Do not introduce external dependencies unless necessary.
4. Keep simulation logic independent of LÖVE.
5. Add tests when adding new core mechanics.
6. Do not use `math.random()` directly.
7. Do not put gameplay logic inside rendering code.
8. Do not add content by hardcoding it into engine logic.
9. Preserve deterministic seeded behavior.
10. Prefer small composable functions over giant game-state functions.
11. Do not implement future systems prematurely.
12. Keep the prototype playable after every major change.

---

 # 47\. Definition of Done — First Playable

 The first playable prototype is complete when a player can:

 1. Start a new run.
2. Receive five coins.
3. Enter an encounter.
4. See coin probabilities.
5. Flip all coins.
6. See Heads/Tails results.
7. See effects resolve.
8. Damage an enemy.
9. Win an encounter.
10. Choose one of three new coins.
11. Add that coin to the collection.
12. Continue into another encounter.
13. Eventually reach a boss.
14. Defeat the boss or lose.
15. Restart with a new seed.

 The prototype does **not** need:

 - Art
- Sound
- Save files
- Meta progression
- Complex map
- Dozens of coins
- Multiple bosses
- Story
- Online features

---

 # 48\. First Implementation Task

 Start by implementing the simulation only.

 The first milestone should be runnable from a command line/test environment and demonstrate:

```
Create seed
↓
Create player
↓
Create 5 coins
↓
Create encounter
↓
Flip coins
↓
Resolve effects
↓
Repeat until victory/defeat
↓
Print complete event log
```

 Example output:

```
Seed: 381927

Encounter started.
Enemy HP: 30

Turn 1

Copper Coin → HEADS
+2 Gold

Sword Coin → TAILS
Nothing

Loaded Coin → HEADS
+4 Gold

Enemy HP: 25

Turn 2

...

Enemy defeated!

VICTORY
Gold: 58
```

 Only after this works should the LÖVE graphical layer be implemented.

---

 # 49\. Guiding Principle

 The implementation should make the following future evolution easy:

```
Simple coin
    ↓
Coin modifiers
    ↓
Probability manipulation
    ↓
Conditional effects
    ↓
Coin synergies
    ↓
Relics
    ↓
Recursive effects
    ↓
Probability-breaking builds
```

 Do not optimize for the final game prematurely.

 Optimize the architecture for **rapid experimentation with game mechanics**.

 The most important question during development is:

 > **Can we add a weird new coin mechanic in a few lines without rewriting the engine?**
