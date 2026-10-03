local Game = require("src.game")
local Serialize = require("src.serialize")

local function equal(actual, expected, message)
  assert(actual == expected, (message or "values differ") .. ": " .. tostring(actual) .. " ~= " .. tostring(expected))
end

local function level(ids)
  Game.characters().test = {name = "Test", description = "", starter = "normal", pool = {}, deck = ids}
  local max = Game.START_MAX
  Game.START_MAX = 10
  local game = Game.new(1, "test", require("content.coin_order"), nil, false)
  Game.START_MAX = max
  local e = game.encounter
  e.quota, e.max_quota, e.combo_step = 1000, 1000, 0
  e.queue, e.pile = {}, {}
  for _, coin in ipairs(game.coins) do e.queue[#e.queue + 1] = coin.uid end
  game.dealt = {uid = e.queue[1], probability = Game.probability(game, game.coins[1])}
  require("src.hooks").bind(game, game.coins[1])
  game.player.energy = 99
  return game
end

local function play(game, side)
  assert(Game.flip(game))
  game.pending.result = side
  assert(Game.resolve(game))
  return game.last_result.gained
end

-- Compost changes only Fortune odds, persists through a shop/save, and applies once per copy per level.
local g = level({"compost", "lucky", "sword"})
equal(play(g, "Tails"), 0)
equal(g.encounter.max_quota, 1002)
assert(math.abs(Game.probability(g, g.coins[2]) - .41) < 1e-9)
assert(math.abs(Game.probability(g, g.coins[3]) - .35) < 1e-9)
equal(g.fortune_bonus, .11)
Game.apply_effect(g, g.coins[1], {type = "fortune_odds", amount = .11})
equal(g.fortune_bonus, .11, "same Compost does not boost twice in one level")
g.phase = "SHOP"
local saved = Game.restore(Serialize.decode(Serialize.encode(Game.snapshot(g))))
assert(saved, "Compost run restores at the shop")
equal(saved.fortune_bonus, .11)
Game.use_modifiers = false
assert(Game.next_encounter(saved))
Game.use_modifiers = true
assert(math.abs(Game.probability(saved, saved.coins[2]) - .41) < 1e-9)
Game.apply_effect(saved, saved.coins[1], {type = "fortune_odds", amount = .11})
equal(saved.fortune_bonus, .22, "Compost boosts again next level")
for _ = 1, 10 do
  saved.encounter_index = saved.encounter_index + 1
  Game.apply_effect(saved, saved.coins[1], {type = "fortune_odds", amount = .11})
end
equal(saved.fortune_bonus, .55, "Fortune boost has a cap")
local legacy = Serialize.decode(Serialize.encode(Game.snapshot(g)))
legacy.fortune_bonus, legacy.encounter.best_scores = nil, nil
assert(Game.restore(legacy), "older saves without new fields still restore")

-- Square Dance scales with deck copies and still receives the usual scoring multiplier.
for copies = 1, 3 do
  local ids = {}
  for i = 1, copies do ids[i] = "square_dance" end
  equal(play(level(ids), "Heads"), 2 * copies * copies, "Square Dance copies")
end
g = level({"megaphone", "square_dance"})
play(g, "Heads")
equal(play(g, "Heads"), 4, "Square Dance takes multiplier")

-- Good Dog fetches the best prior scorer, uses the shared return cap, and acts once per level.
g = level({"sword", "normal", "good_dog", "good_dog"})
local sword_uid = g.coins[1].uid
play(g, "Heads")
play(g, "Heads")
equal(play(g, "Heads"), 2)
equal(g.encounter.returned, 1)
local fetched_sword = false
for _, uid in ipairs(g.encounter.queue) do if uid == sword_uid then fetched_sword = true end end
for _, uid in ipairs(g.encounter.pile) do if uid == sword_uid then fetched_sword = true end end
assert(fetched_sword, "the top scorer returns to the bank")
equal(play(g, "Heads"), 2)
equal(g.encounter.returned, 2, "a separate Good Dog copy may fetch")
Game.apply_effect(g, g.coins[3], {type = "fetch_best"})
equal(g.encounter.returned, 2, "one Good Dog copy only fetches once per level")
g.encounter.returned = Game.RETURN_CAP
Game.apply_effect(g, g.coins[4], {type = "fetch_best"})
equal(g.encounter.returned, Game.RETURN_CAP, "shared return cap applies")

print("new coin tests passed")
