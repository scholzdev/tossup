-- Endless mode after the boss, plus Amplifier, True Echo and Doubler.
local Game = require("src.game")

local function equal(a, b, message) assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b)) end

local function level(ids, seed)
  Game.characters().test = {name = "Test", description = "", starter = "normal", pool = {}}
  local plain = {}
  for i = 1, #ids do plain[i] = "normal" end
  local keep = Game.START_MAX
  Game.START_MAX = 10 -- tests use decks bigger than the start size
  Game.characters().test.deck = plain -- constructed test stacks may exceed deckbuilding copy limits
  local game = Game.new(seed or 1, "test", require("content.coin_order"), nil, false)
  Game.START_MAX = keep
  for i, id in ipairs(ids) do game.coins[i].id = id end
  local e = game.encounter
  e.quota, e.max_quota = 1000, 1000
  e.combo_step = 0
  e.queue, e.pile = {}, {}
  for _, coin in ipairs(game.coins) do e.queue[#e.queue + 1] = coin.uid end
  game.dealt = {uid = e.queue[1], probability = .5}
  require("src.hooks").bind(game, game.coins[1]) -- the first coin is dealt by hand, so bind its hooks here
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

-- stages: the route, then endless levels that grow
equal(Game.stage(4).boss, true)
local s5, s6 = Game.stage(5), Game.stage(6)
equal(s5.endless, 1)
equal(s6.endless, 2)
assert(s6.per_coin > s5.per_coin and s5.per_coin > Game.stage(4).per_coin, "endless quotas grow")
assert(s5.inverts and s5.payout and s6.payout > s5.payout)
equal(Game.quota_for(5, 10), math.floor(Game.stage(5).per_coin * 10 + .5))

-- boss -> victory -> continue -> shop -> endless level 5
local g = Game.new(3, "blade")
g.encounter_index = 3 -- the shop before the boss
g.phase = "SHOP"
assert(Game.next_encounter(g))
equal(g.encounter.boss, true)
g.encounter.cleared = true
g.encounter.quota = 0
g.dealt = nil
assert(Game.end_level(g))
equal(g.phase, "VICTORY")
assert(Game.continue_endless(g), "can continue after the boss")
equal(g.phase, "SHOP")
assert(not Game.continue_endless(g), "only once")
assert(Game.leave_shop(g))
equal(g.encounter_index, 5)
equal(g.phase, "ENCOUNTER")
equal(g.encounter.endless, 1)
equal(g.encounter.boss, false)
equal(g.encounter.inverts, true)
-- the 5th flip of an endless level inverts, like the boss
g.mulligan = nil
g.encounter.flips = 4
g.encounter.quota, g.encounter.max_quota = 1000, 1000
g.encounter.queue = {g.coins[1].uid}
g.dealt = {uid = g.coins[1].uid, probability = 1}
assert(Game.flip(g))
equal(g.pending.raw, "Heads")
equal(g.pending.result, "Tails", "5th flip inverted")

-- amplifier: buffs last one coin longer and get stronger (x2 -> x3)
g = level({"megaphone", "amplifier", "sword", "sword", "sword", "sword"})
scored(g, "Heads") -- megaphone: next 2 coins x2
equal(g.encounter.buffs[1].amount, 2)
equal(scored(g, "Heads"), 2, "amplifier itself is doubled: 1 point x2")
equal(g.encounter.buffs[1].amount, 3, "x2 became x3")
equal(g.encounter.buffs[1].left, 2, "megaphone's second coin plus one more")
equal(scored(g, "Heads"), 9, "sword x3")
equal(scored(g, "Heads"), 9, "sword x3 again")
equal(scored(g, "Heads"), 3, "buff over")

-- true echo: repeats what the previous coin really did (Bettor's computed points, which Echo would miss), on either side
g = level({"dagger", "dagger", "bettor", "true_echo"})
scored(g, "Heads"); scored(g, "Heads")
equal(scored(g, "Heads"), 9, "bettor: 3 x 3")
equal(scored(g, "Tails"), 9, "true echo repeats the 9 even on Tails")

-- true echo repeats buffs too
g = level({"megaphone", "true_echo", "sword", "sword", "sword"})
scored(g, "Heads")
scored(g, "Heads")
equal(g.encounter.buffs[#g.encounter.buffs].kind, "mult", "echoed buff")

-- doubler: 3, 6, 12 across copies; counter resets each level and counts Tails flips too
g = level({"doubler", "doubler", "doubler", "doubler"})
equal(scored(g, "Heads"), 3)
equal(scored(g, "Heads"), 6)
equal(scored(g, "Tails"), 0, "tails flip pays nothing but counts")
equal(scored(g, "Heads"), 24, "fourth doubler flip: 3 x 2^3")

print("endless tests passed")
