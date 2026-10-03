local Game = require("src.game")

local function equal(a, b, label)
  assert(a == b, (label or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b))
end

local function level(ids)
  Game.characters().test = {name = "Test", description = "", starter = "normal", pool = {}, deck = ids}
  local old_max = Game.START_MAX
  Game.START_MAX = 10
  local game = Game.new(1, "test", require("content.coin_order"), nil, false)
  Game.START_MAX = old_max
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
  return game.last_result
end

-- Matching coins consume type buffs. Untyped coins leave them waiting.
local g = level({"whetstone", "normal", "sword", "dagger", "sword"})
play(g, "Heads")
play(g, "Tails")
equal(g.encounter.buffs[1].left, 2, "normal does not consume Steel")
equal(play(g, "Heads").gained, 6, "Sword gains three points")
equal(play(g, "Tails").penalty, 2, "Dagger adds two quota")
equal(play(g, "Heads").gained, 3, "Steel buff expires after two matching coins")
g = level({"whetstone", "echo", "sword"})
play(g, "Heads")
play(g, "Heads")
equal(play(g, "Heads").gained, 9, "Echo copies a type buff with its type intact")

-- Blood's Edge receives half of both risks and rewards.
g = level({"blood_pact", "blood"})
play(g, "Heads")
local edge = play(g, "Tie")
equal(edge.gained, 9, "Blood Edge scores 5 + 4")
equal(edge.penalty, 5, "Blood Edge raises quota 3 + 2")
g = level({"blood_pact", "blood"})
play(g, "Heads")
equal(play(g, "Tails").penalty, 10, "Blood Tails adds six plus four quota")

-- Loaded belongs to Fortune and Greed; the latter doubles income, then a Tails costs gold.
local loaded = Game.catalog().loaded
equal(loaded.probability, .59, "Loaded Heads chance")
equal(loaded.tie_probability, .23, "Loaded Edge chance")
equal(Game.tie_effects("loaded")[1].amount, 4, "Loaded Edge takes half the Tails loss first")
equal(Game.tie_effects("loaded")[2].amount, 2, "Loaded Edge pays half its Heads gold")
g = level({"loaded"})
local edge_gold = g.player.gold
play(g, "Tie")
equal(g.player.gold, edge_gold - 2, "Loaded Edge is a net loss when gold is held")
g = level({"loaded"})
g.player.gold = 20
play(g, "Tails")
equal(g.player.gold, 12, "Loaded Tails loses eight gold")
g = level({"loaded"})
play(g, "Tails")
equal(g.player.gold, 0, "Loaded loss stops at zero gold")
g = level({"loaded"})
g.fortune_bonus = .55
equal(Game.probability(g, g.coins[1]), .77, "Fortune boosts stop below Loaded's Edge chance")
g = level({"counterfeiter", "loaded", "copper", "copper"})
play(g, "Heads")
local gold = g.player.gold
play(g, "Heads")
equal(g.player.gold, gold + 8, "Loaded's gold doubles")
g = level({"counterfeiter", "loaded"})
play(g, "Heads")
gold = g.player.gold
play(g, "Tie")
equal(g.player.gold, gold, "Counterfeiter doubles the Edge payout, not its loss")
g = level({"counterfeiter", "loaded", "copper", "copper"})
play(g, "Heads")
gold = g.player.gold
play(g, "Heads")
play(g, "Tails")
equal(g.player.gold, gold + 5, "Greed Tails costs three gold")
play(g, "Tails")
equal(g.player.gold, gold + 5, "Greed buff expires after two matching coins")
g = level({"counterfeiter", "bounty"})
play(g, "Heads")
gold = g.player.gold
play(g, "Heads")
equal(g.player.gold, gold + 4, "Bounty's earned gold doubles too")

-- Chaos doubles a penalty as well as a reward, without rerolling the coin.
g = level({"doppelganger", "cursed"})
play(g, "Heads")
equal(play(g, "Tails").penalty, 4, "Cursed penalty is doubled")
g = level({"doppelganger", "cursed"})
play(g, "Heads")
equal(play(g, "Heads").gained, 30, "Cursed reward is doubled")

-- Rhythm pays for an intact combo and punishes a real break.
g = level({"conductor", "normal", "chain"})
play(g, "Heads")
play(g, "Heads")
equal(play(g, "Heads").gained, 10, "Chain gets four extra combo points")
g = level({"conductor", "normal", "chain"})
play(g, "Heads")
play(g, "Tails")
equal(play(g, "Heads").penalty, 5, "broken combo adds five quota")

print("type combo tests passed")
