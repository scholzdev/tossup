-- Combo: consecutive identical results multiply points; Hot Hand, Anchor, Bettor, Cash Out, Cold Streak and the Baton relic.
local Game = require("src.game")
local Signal = require("src.signal")

local function equal(a, b, message) assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b)) end

local function level(ids, seed)
  Game.characters().test = {name = "Test", description = "", starter = "normal", pool = {}}
  local plain = {}
  for i = 1, #ids do plain[i] = "normal" end
  local keep = Game.START_MAX
  Game.START_MAX = 10 -- tests use decks bigger than the start size
  local game = Game.new(seed or 1, "test", require("content.coin_order"), plain, false)
  Game.START_MAX = keep
  for i, id in ipairs(ids) do game.coins[i].id = id end -- the copy limit only applies when a deck is built
  local e = game.encounter
  e.quota, e.max_quota = 1000, 1000
  e.queue, e.pile = {}, {}
  for _, coin in ipairs(game.coins) do e.queue[#e.queue + 1] = coin.uid end
  game.dealt = {uid = e.queue[1], probability = .5}
  game.player.energy = 99
  return game
end

local function scored(game, side)
  local before = game.encounter.scored
  assert(Game.flip(game))
  game.pending.result = side
  assert(Game.resolve(game))
  return game.encounter.scored - before
end

-- the multiplier: x1, x1.25, x1.5, x1.75 on a 4-point coin, rounded
local g = level({"dagger", "dagger", "dagger", "dagger", "dagger"})
equal(scored(g, "Heads"), 4, "first flip x1")
equal(scored(g, "Heads"), 5, "second flip x1.25")
equal(scored(g, "Heads"), 6, "third flip x1.5")
equal(scored(g, "Heads"), 7, "fourth flip x1.75")
equal(g.encounter.combo_len, 4)
equal(scored(g, "Tails"), 1, "a different result breaks the combo (Tails pays 1)")
equal(g.encounter.combo_len, 1)
equal(g.encounter.combo_side, "Tails")

-- cap at x3
g = level({"dagger", "dagger"})
g.encounter.combo_side, g.encounter.combo_len = "Heads", 30
equal(scored(g, "Heads"), 12, "capped at x3")

-- hot hand adds an extra step for the next flip
g = level({"hot_hand", "dagger"})
equal(scored(g, "Heads"), 2)
equal(g.encounter.combo_len, 2, "hot hand: 1 for the flip + 1 extra")
equal(scored(g, "Heads"), 6, "next flip is the third step: x1.5")

-- anchor: a shield lets the next different result pass without breaking the combo
g = level({"anchor", "dagger", "dagger"})
equal(scored(g, "Heads"), 2)
equal(g.encounter.shield, 1)
equal(scored(g, "Tails"), 1, "tails pays, combo held")
equal(g.encounter.shield, 0)
equal(g.encounter.combo_side, "Heads")
equal(g.encounter.combo_len, 1)

-- bettor: 3 points per flip in the combo, then the multiplier on top
g = level({"dagger", "dagger", "bettor"})
scored(g, "Heads"); scored(g, "Heads")
equal(scored(g, "Heads"), math.floor(9 * 1.5 + .5), "bettor: 3 x 3 = 9, x1.5")

-- cash out: the multiplier counts twice, then the combo resets
g = level({"dagger", "dagger", "cash_out"})
scored(g, "Heads"); scored(g, "Heads")
equal(scored(g, "Heads"), math.floor(3 * 1.5 * 1.5 + .5), "cash out squares the multiplier")
equal(g.encounter.combo_len, 0, "combo reset")

-- cold streak: Tails pays per Tails in a row
g = level({"normal", "cold_streak", "cold_streak"})
scored(g, "Tails")
equal(scored(g, "Tails"), math.floor(4 * 1.25 + .5), "2 x 2 Tails, x1.25")

-- baton: bigger step and cap once the level starts
g = level({"normal"})
Game.add_relic(g, "baton")
Signal.emit("encounter_start", {game = g, encounter = g.encounter})
equal(g.encounter.combo_step, 0.4)
equal(g.encounter.combo_cap, 4)

print("combo tests passed")
