-- "Next N coins" buffs: Megaphone (x2), Cheerleader (+odds), Mirror (swap sides), Domino (forced Heads), plus Pot, Twin and the Metronome relic.
local Game = require("src.game")

local function equal(a, b, message) assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b)) end

-- a level whose bank holds the given coins in order; quota is huge so nothing ends
local function level(ids, seed)
  Game.characters().test = {name = "Test", description = "", starter = "normal", pool = {}, deck = ids}
  local keep = Game.START_MAX
  Game.START_MAX = 10 -- tests use decks bigger than the start size
  local game = Game.new(seed or 1, "test", require("content.coin_order"), nil, false)
  Game.START_MAX = keep
  local e = game.encounter
  e.quota, e.max_quota = 1000, 1000
  e.combo_step = 0 -- the combo has its own tests (test_combo.lua)
  local by_id = {}
  for _, coin in ipairs(game.coins) do by_id[#by_id + 1] = coin.uid end
  e.queue, e.pile = {}, {}
  for _, uid in ipairs(by_id) do e.queue[#e.queue + 1] = uid end
  game.dealt = {uid = e.queue[1], probability = Game.probability(game, game.coins[1])}
  game.player.energy = 99
  return game
end

local function play(game, side)
  assert(Game.flip(game))
  if side then game.pending.result = side end
  assert(Game.resolve(game))
end

local function scored(game, side)
  local before = game.encounter.scored
  play(game, side)
  return game.encounter.scored - before
end

-- megaphone: the next 2 coins pay double, the third does not
local g = level({"megaphone", "sword", "sword", "sword"})
equal(scored(g, "Heads"), 2, "megaphone itself is not doubled")
equal(scored(g, "Heads"), 6, "next coin x2")
equal(scored(g, "Heads"), 6, "second coin x2")
equal(scored(g, "Heads"), 3, "third coin back to normal")

-- cheerleader: +20% Heads on the next 2 coins (shown in the odds), then gone
g = level({"cheerleader", "sword", "sword", "sword"})
play(g, "Heads")
assert(math.abs(Game.probability(g, Game.get_coin(g, g.dealt.uid)) - .55) < 1e-9, "next coin has +20%")
play(g, "Tails")
assert(math.abs(Game.probability(g, Game.get_coin(g, g.dealt.uid)) - .55) < 1e-9, "second coin has +20%")
play(g, "Tails")
assert(math.abs(Game.probability(g, Game.get_coin(g, g.dealt.uid)) - .35) < 1e-9, "third coin back to base odds")

-- Focus prepares one risky scorer instead of raising its own odds after it has been played.
g = level({"focus", "sword", "sword"})
play(g, "Heads")
assert(math.abs(Game.probability(g, Game.get_coin(g, g.dealt.uid)) - .7) < 1e-9, "Focus boosts the next coin")
play(g, "Tails")
assert(math.abs(Game.probability(g, Game.get_coin(g, g.dealt.uid)) - .35) < 1e-9, "Focus boost lasts one coin")

-- mirror: Heads makes the next coin use its other side
g = level({"mirror", "dagger", "dagger"})
play(g, "Heads")
equal(scored(g, "Heads"), 1, "dagger Heads used its Tails effect (1)")
equal(scored(g, "Heads"), 2, "buff used up")

-- domino: the next coin lands Heads whatever the roll
for seed = 1, 12 do
  g = level({"domino", "sword", "sword"}, seed)
  play(g, "Heads")
  assert(Game.flip(g))
  equal(g.pending.result, "Heads", "domino forces Heads (seed " .. seed .. ")")
  Game.resolve(g)
end

-- pot: Heads pays the number of flips so far (this flip included)
g = level({"normal", "normal", "pot"})
play(g, "Tails")
play(g, "Tails")
equal(scored(g, "Heads"), 3, "pot after 3 flips")

-- twin: copies the previous flip's side
for seed = 1, 6 do
  g = level({"normal", "twin"}, seed)
  play(g, "Tails")
  assert(Game.flip(g))
  equal(g.pending.result, "Tails", "twin after Tails")
end

-- metronome: every 4th flip pays double
g = level({"normal", "normal", "normal", "normal", "normal"})
Game.add_relic(g, "metronome")
local sum = {}
for i = 1, 5 do sum[i] = scored(g, "Heads") end
equal(sum[3], 1, "third flip normal")
equal(sum[4], 2, "fourth flip doubled")
equal(sum[5], 1, "fifth flip normal")

print("buff tests passed")
