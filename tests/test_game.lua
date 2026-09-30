local Game = require("src.game")
local RNG = require("src.rng")

-- most tests below assume the old one-coin, six-coin-pool characters; the real content is
-- checked at the end
local chars = Game.characters()
local real = {}
for id, def in pairs(chars) do real[id] = {def.starter, def.deck, def.pool, def.locked} end
local legacy = {
  blade = {"sword", {"sword", "dagger", "hammer", "blood", "cursed", "focus"}},
  seer = {"cursed", {"cursed", "lucky", "focus", "spark", "blood", "dagger"}},
  trader = {"dagger", {"dagger", "copper", "loaded", "spark", "hammer", "sword"}},
}
for id, spec in pairs(legacy) do
  chars[id].starter, chars[id].deck, chars[id].pool, chars[id].locked = spec[1], nil, spec[2], nil
end

local function equal(a, b, message)
  assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b))
end

local function drive(seed, character)
  local game = Game.new(seed, character)
  local steps = 0
  while game.phase ~= "VICTORY" and game.phase ~= "GAME_OVER" do
    steps = steps + 1
    assert(steps < 120, "run did not terminate")
    if game.phase == "ENCOUNTER" then
      assert(Game.flip(game))
      assert(Game.resolve(game))
    elseif game.phase == "SHOP" then
      assert(Game.leave_shop(game))
    end
  end
  return game
end

local a, b = drive(381927, "blade"), drive(381927, "blade")
equal(a.phase, b.phase, "same seed outcome")
equal(a.rng_state, b.rng_state, "same seed RNG state")
equal(table.concat(a.log, "\n"), table.concat(b.log, "\n"), "same seed event log")

local r1, r2 = {rng_state = RNG.seed(12)}, {rng_state = RNG.seed(12)}
for _ = 1, 20 do equal(RNG.random(r1), RNG.random(r2), "RNG sequence") end
local sample = {rng_state = RNG.seed(123)}
local heads = 0
for _ = 1, 100000 do if RNG.random(sample) < .5 then heads = heads + 1 end end
assert(heads > 49000 and heads < 51000, "50% distribution: " .. heads)

local g = Game.new(7)
equal(#g.coins, 1, "one starting coin")
equal(g.coins[1].id, "sword", "Blade starter")
equal(g.encounter.draws, 10, "opening draw budget")
assert(not Game.resolve(g), "nothing to resolve before flip")
assert(Game.flip(g))
equal(g.pending.uid, g.coins[1].uid, "one drawn coin")
assert(not Game.flip(g), "cannot draw a second coin before resolving")
assert(Game.reroll(g))
equal(g.player.energy, 2)
assert(Game.force(g, "Heads"))
equal(g.player.energy, 0)
assert(not Game.force(g, "Tails"), "insufficient energy")
g.encounter.quota = 15
local hp = g.encounter.quota
assert(Game.resolve(g))
equal(g.encounter.quota, hp - 5, "Sword Heads scores points")
equal(g.encounter.draws, 9, "one draw consumed")
equal(g.encounter.flips, 1)
assert(not Game.resolve(g), "cannot resolve twice")
assert(Game.flip(g), "single-coin deck reshuffles")
equal(g.pending.uid, g.coins[1].uid)

local seer = Game.new(13, "seer")
local trader = Game.new(13, "trader")
equal(seer.coins[1].id, "cursed")
equal(trader.coins[1].id, "dagger")
assert(Game.flip(seer))
seer.encounter.quota = 0
assert(Game.resolve(seer))
equal(seer.phase, "SHOP", "win goes straight to the shop")
local pool = {}
for _, id in ipairs(Game.characters().seer.pool) do pool[id] = true end
for _, id in ipairs(seer.shop_offers) do assert(pool[id] or id == "normal", "offer must be in character pool") end

local win = Game.new(1)
assert(Game.flip(win))
win.encounter.quota = 1
assert(Game.force(win, "Heads"))
assert(Game.resolve(win))
equal(win.phase, "SHOP", "enemy death wins")
equal(win.player.gold, 30, "level payout")
equal(#win.coins, 1, "no free coin")
equal(#win.shop_offers, 4)
win.player.gold = 100
local selected = win.coins[1]
local old = Game.probability(win, selected)
assert(Game.upgrade(win, selected.uid))
equal(Game.probability(win, selected), old + .10, "permanent probability upgrade")
assert(Game.buy_energy(win))
equal(win.player.max_energy, 4)
assert(Game.reroll_shop(win))
assert(Game.buy(win, 1))
equal(#win.coins, 2)
assert(Game.remove(win, selected.uid))
equal(#win.coins, 1)
assert(Game.leave_shop(win))
equal(win.encounter_index, 2)
equal(win.phase, "ENCOUNTER")

local cap = Game.new(5)
cap.phase = "SHOP"
cap.player.gold = 100
for _, id in ipairs({"dagger", "hammer", "blood", "focus"}) do
  cap.shop_offers = {id}
  assert(Game.buy(cap, 1))
end
equal(#cap.coins, 5, "small deck cap")
local replaced_uid = cap.coins[1].uid
assert(Game.select(cap, replaced_uid))
cap.shop_offers = {"spark"}
assert(Game.buy(cap, 1))
equal(#cap.coins, 5, "buy replaces selected coin at cap")
assert(not Game.get_coin(cap, replaced_uid), "old coin removed")
assert(Game.leave_shop(cap))
cap.encounter.quota = 10000
local seen = {}
for _ = 1, 5 do
  assert(Game.flip(cap))
  assert(not seen[cap.pending.uid], "each deck coin drawn once before reshuffle")
  seen[cap.pending.uid] = true
  assert(Game.resolve(cap))
end
assert(Game.flip(cap), "deck reshuffles after one full cycle")
assert(seen[cap.pending.uid], "reshuffle draws owned coin")

local loss = Game.new(2)
loss.encounter.quota = 10000
for _ = 1, 10 do
  assert(Game.flip(loss))
  assert(Game.resolve(loss))
end
equal(loss.phase, "GAME_OVER", "draw exhaustion loses")

local lucky = Game.new(9)
lucky.coins[1].id = "lucky"
lucky.encounter.draws = 1
lucky.player.energy = 100
for _ = 1, 4 do
  assert(Game.flip(lucky))
  assert(Game.force(lucky, "Heads"))
  assert(Game.resolve(lucky))
end
equal(lucky.encounter.bonus_draws, 3, "extra draws capped")
equal(lucky.phase, "GAME_OVER", "extra draws cannot make an endless level")

local boss = Game.new(4)
boss.encounter.boss = true
boss.encounter.flips = 4
assert(Game.flip(boss))
assert(Game.force(boss, "Heads"))
local boss_hp = boss.encounter.quota
assert(Game.resolve(boss))
equal(boss.last_result.final, "Tails", "fifth boss draw inverts")
equal(boss.encounter.quota, boss_hp, "inverted Sword Heads scores nothing")

local function play(game, side)
  assert(Game.flip(game))
  game.pending.result = side
  assert(Game.resolve(game))
end

local penny = Game.new(5)
Game.add_relic(penny, "penny")
penny.encounter.quota = 1000
play(penny, "Tails")
equal(penny.last_result.final, "Heads", "penny converts first Tails")
play(penny, "Tails")
equal(penny.last_result.final, "Tails", "penny only once per encounter")

local clock = Game.new(6)
Game.add_relic(clock, "clock")
clock.encounter.quota = 1000
clock.encounter.flips = 9
play(clock, "Tails")
equal(clock.last_result.final, "Heads", "clock makes 10th flip Heads")

local magnet = Game.new(8)
Game.add_relic(magnet, "magnet")
magnet.encounter.quota = 1000
magnet.encounter.draws = 100
for _ = 1, 3 do play(magnet, "Heads") end
assert(math.abs(magnet.encounter.magnet - .05) < 1e-9, "magnet after 3 Heads")
play(magnet, "Tails")
equal(magnet.encounter.streak, 0, "Tails resets streak")
assert(Game.probability(magnet, magnet.coins[1]) > .5, "magnet raises odds")

local shop = Game.new(9)
shop.phase = "SHOP"
shop.shop_relic = "magnet"
shop.player.gold = 25
assert(Game.buy_relic(shop))
equal(shop.relics[1], "magnet", "relic bought")
assert(not Game.buy_relic(shop), "one relic per shop slot")

local d = Game.new(11)
assert(not Game.discard(d), "cannot discard the only coin")
local d2 = Game.new(12)
d2.coins[2] = {uid = 99, id = "copper", bonus = 0}
d2.encounter.pile = {}
d2.encounter.discarded = {}
d2.encounter.quota = 1000
d2.dealt = {uid = 99, probability = .5}
assert(Game.discard(d2))
equal(d2.player.energy, 2, "discard costs energy")
equal(d2.dealt.uid, d2.coins[1].uid, "next coin dealt")
assert(not Game.discard(d2), "cannot discard last remaining coin")
assert(Game.flip(d2) and Game.resolve(d2))
equal(d2.dealt.uid, d2.coins[1].uid, "discarded coin never returns")

for id, r in pairs(real) do chars[id].starter, chars[id].deck, chars[id].pool, chars[id].locked = r[1], r[2], r[3], r[4] end

-- real content: Blade starts with three Normal coins and the bank tops up to five in the shop
local blade = Game.new(3, "blade")
equal(#blade.coins, 3)
for _, c in ipairs(blade.coins) do equal(c.id, "normal") end
blade.encounter.quota = 1
assert(Game.flip(blade))
blade.pending.result = "Heads"
assert(Game.resolve(blade))
equal(blade.phase, "SHOP", "win opens the shop")
equal(blade.cleared, 1)
equal(blade.shop_offers[1], "normal", "normal coin always offered first")
for _, id in ipairs(blade.shop_offers) do
  assert(id == "normal" or id == "sword" or id == "dagger", "locked coin leaked into shop: " .. id)
end
blade.player.gold = 10
assert(Game.buy(blade, 1))
equal(blade.player.gold, 5, "normal coin costs 5")
blade.shop_offers[1] = "normal"
blade.player.gold = 5
assert(Game.buy(blade, 1))
equal(#blade.coins, 5, "bank topped up to five")
equal(Game.run_tokens(blade), 1, "one token per level cleared")
blade.phase = "VICTORY"
equal(Game.run_tokens(blade), 4, "boss bonus")

-- unlocked extras join the shop pool
local unlocked = Game.new(3, "blade", {"hammer"})
unlocked.phase = "SHOP"
local found = false
for seed = 1, 40 do
  unlocked.rng_state = seed
  unlocked.player.gold = 100
  assert(Game.reroll_shop(unlocked))
  for _, id in ipairs(unlocked.shop_offers) do if id == "hammer" then found = true end end
end
assert(found, "unlocked coin can appear in the shop")

-- a penalty (negative effect) raises the quota instead of hurting a player
local pen = Game.new(21, "blade")
pen.encounter.quota = 10
pen.encounter.max_quota = 10
Game.apply_effect(pen, nil, {type = "penalty", amount = 4})
equal(pen.encounter.quota, 14, "penalty raises the remaining quota")
equal(pen.encounter.max_quota, 14, "and the total")
equal(pen.player.hp, nil, "player has no HP")

print("game tests passed")
