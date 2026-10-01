-- Level modifiers, the newer coins (Jackpot, Mimic, Orchestra, Lifeline, Horoscope, Crystal Ball) and the newer chips.
local Game = require("src.game")
local Items = require("src.items")

local function equal(a, b, message) assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b)) end

local function level(ids, seed)
  Game.characters().test = {name = "Test", description = "", starter = "normal", pool = {}}
  local plain = {}
  for i = 1, #ids do plain[i] = "normal" end
  local keep = Game.START_MAX
  Game.START_MAX = 10
  local game = Game.new(seed or 1, "test", require("content.coin_order"), plain, false)
  Game.START_MAX = keep
  for i, id in ipairs(ids) do game.coins[i].id = id end
  local e = game.encounter
  e.quota, e.max_quota = 1000, 1000
  e.combo_step = 0
  e.queue, e.pile = {}, {}
  for _, coin in ipairs(game.coins) do e.queue[#e.queue + 1] = coin.uid end
  game.dealt = {uid = e.queue[1], probability = .5}
  require("src.hooks").bind(game, game.coins[1])
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

-- modifiers: level 1 has none, every later level has one, and each one does what it says
Game.use_modifiers = true
local g = Game.new(5, "blade")
equal(g.encounter.modifier, nil, "level 1 has no modifier")
g.phase = "SHOP"
assert(Game.next_encounter(g))
assert(g.encounter.modifier, "level 2 has a modifier")
local mods = Game.modifiers()
assert(mods[g.encounter.modifier], "it is a known modifier")
local function fresh(id)
  local game = Game.new(6, "blade")
  game.encounter.payout = 100
  game.encounter.quota, game.encounter.max_quota = 20, 20
  mods[id].apply(game, game.encounter)
  return game
end
assert(math.abs(fresh("lucky_day").encounter.magnet - .1) < 1e-9)
assert(math.abs(fresh("cold_snap").encounter.magnet + .1) < 1e-9)
equal(fresh("cold_snap").encounter.payout, 140)
equal(fresh("power_surge").player.energy, 5)
equal(fresh("blackout").player.energy, 1)
equal(fresh("blackout").encounter.payout, 140)
equal(fresh("high_stakes").encounter.quota, 25)
equal(fresh("high_stakes").encounter.payout, 150)
equal(fresh("good_rhythm").encounter.combo_step, .4)
equal(Game.exchanges_left(fresh("bonus_exchange")), Game.EXCHANGE_MAX + 1)
local rush = level({"loaded", "normal"})
rush.encounter.gold_rush = nil
rush.encounter.gold_mult = 2
local gold = rush.player.gold
scored(rush, "Heads")
equal(rush.player.gold, gold + 8, "gold rush doubles Loaded's 4 gold")
Game.use_modifiers = false

-- Jackpot: 20% for 25
equal(Game.catalog().jackpot.probability, .2)
equal(scored(level({"jackpot"}), "Heads"), 25)

-- Mimic copies the Heads effects of another coin in the deck
equal(scored(level({"mimic", "sword"}), "Heads"), 5, "mimic copies Sword's 5 points")
equal(scored(level({"mimic", "sword"}), "Tails"), 1, "tails: 1 point")
equal(scored(level({"mimic"}), "Heads"), 0, "nothing to copy")

-- Orchestra: 2 points per different coin type
equal(scored(level({"orchestra", "sword", "dagger", "sword"}), "Heads"), 6, "three types in the deck")

-- Lifeline: one more exchange this level
local life = level({"lifeline", "normal"})
equal(Game.exchanges_left(life), Game.EXCHANGE_MAX)
scored(life, "Heads")
equal(Game.exchanges_left(life), Game.EXCHANGE_MAX + 1)

-- Horoscope: all coins get more Heads chance for the rest of the level
local horo = level({"horoscope", "normal"})
scored(horo, "Heads")
assert(math.abs(horo.encounter.magnet - .07) < 1e-9)
assert(math.abs(Game.probability(horo, horo.coins[2]) - .57) < 1e-9)

-- Crystal Ball: Heads 5 points; Tails lets you discard one of the next three bank coins (once)
local ball = level({"crystal_ball", "sword", "dagger", "normal", "normal"})
scored(ball, "Tails")
equal(ball.encounter.bank_discards, 1)
local victim = ball.encounter.queue[2]
assert(Game.discard_bank(ball, victim))
assert(ball.encounter.discarded[victim])
assert(not Game.discard_bank(ball, ball.encounter.queue[2]), "only one discard per Tails")

-- chips
local chips = level({"normal", "normal", "normal"})
chips.items = {"energy_drink", "shortcut", "safety_net", "lucky_charm"}
chips.player.energy = 1
assert(Items.use(chips, 1))
equal(chips.player.energy, 3, "Energy Drink: +2")
local before = chips.encounter.scored
assert(Items.use(chips, 1))
equal(chips.encounter.scored - before, 3, "Shortcut: +3 points")
assert(Items.use(chips, 1))
equal(chips.encounter.shield, 1, "Safety Net: a shield")
assert(Items.use(chips, 1))
assert(math.abs(chips.dealt.probability - .7) < 1e-9, "Lucky Charm applies to the coin in play")
scored(chips, "Tails")
assert(math.abs(Game.probability(chips, Game.get_coin(chips, chips.dealt.uid)) - .7) < 1e-9, "and to the next coin")
scored(chips, "Tails")
assert(math.abs(Game.probability(chips, Game.get_coin(chips, chips.dealt.uid)) - .5) < 1e-9, "then it is used up (2 coins)")

print("extra tests passed")
