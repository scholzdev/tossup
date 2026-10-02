local Game = require("src.game")
local Profile = require("src.profile")
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
      if game.dealt then
        assert(Game.flip(game))
        assert(Game.resolve(game))
      else
        assert(Game.can_exchange(game), "an empty stack can only be exchanged")
        Game.exchange(game)
      end
      if game.phase == "ENCOUNTER" and game.encounter.cleared then Game.end_level(game) end
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
equal(Game.coins_left(g), 1, "the stack holds every coin at level start")
g.reshuffle = true -- this test plays the single coin repeatedly
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
equal(g.encounter.quota, hp - 3, "Sword Heads scores points")
equal(g.encounter.flips, 1)
assert(not Game.resolve(g), "cannot resolve twice")
assert(Game.flip(g), "single-coin deck reshuffles")
equal(g.pending.uid, g.coins[1].uid)

local seer = Game.new(13, "seer")
local trader = Game.new(13, "trader")
equal(seer.coins[1].id, "cursed")
equal(trader.coins[1].id, "dagger")
seer.reshuffle = true -- one-coin deck: keep the level open after clearing
assert(Game.flip(seer))
seer.encounter.quota = 0
assert(Game.resolve(seer))
equal(seer.phase, "ENCOUNTER", "quota met: the level stays open")
assert(seer.encounter.cleared)
assert(Game.end_level(seer))
equal(seer.phase, "SHOP", "opening the shop ends the level")
local pool = {}
for _, id in ipairs(Game.characters().seer.pool) do pool[id] = true end
for _, id in ipairs(seer.shop_offers) do assert(pool[id] or id == "normal", "offer must be in character pool") end

local win = Game.new(1)
win.reshuffle = true
assert(Game.flip(win))
win.encounter.quota = 1
assert(Game.force(win, "Heads"))
assert(Game.resolve(win))
assert(win.encounter.cleared, "quota met")
assert(Game.end_level(win))
equal(win.phase, "SHOP", "enemy death wins")
equal(win.player.gold, Game.START_GOLD + Game.route[1].payout + 1, "level payout plus 1 gold for 2 points beyond the quota")
equal(#win.coins, 1, "no free coin")
equal(#win.shop_offers, 4)
win.player.gold = 100
local selected = win.coins[1]
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
cap.player.gold = 1000
-- deck slots: the deck starts with START_MAX slots and the shop sells the rest one by one
equal(cap.slots, Game.START_MAX)
for _ = #cap.coins + 1, Game.START_MAX do
  cap.shop_offers = {"dagger"}
  assert(Game.buy(cap, 1))
end
equal(#cap.coins, Game.START_MAX)
cap.shop_offers = {"dagger"}
assert(not Game.buy(cap, 1), "no free slot: buying a coin is refused")
local gold_slots = cap.player.gold
for i = Game.START_MAX + 1, Game.DECK_MAX do
  assert(Game.buy_slot(cap), "buy slot " .. i)
  equal(cap.slots, i)
end
local slot_total = 0
for i = 0, Game.DECK_MAX - Game.START_MAX - 1 do slot_total = slot_total + Game.SLOT_COST + Game.SLOT_STEP * i end
equal(cap.player.gold, gold_slots - slot_total, "each slot costs SLOT_STEP more than the one before")
equal(Game.slot_cost(Game.new(6)), Game.SLOT_COST, "the first extra slot costs SLOT_COST")
assert(not Game.buy_slot(cap), "no slot beyond DECK_MAX")
local poor = Game.new(6)
poor.phase = "SHOP"
poor.player.gold = Game.SLOT_COST - 1
assert(not Game.buy_slot(poor), "not enough gold")
equal(poor.slots, Game.START_MAX)
for i = Game.START_MAX + 1, Game.DECK_MAX do
  cap.shop_offers = {({"dagger", "hammer", "blood", "focus", "spark", "sword", "loaded"})[(i - 1) % 7 + 1]}
  assert(Game.buy(cap, 1))
end
equal(#cap.coins, Game.DECK_MAX, "deck cap")
local gold_before = cap.player.gold
cap.shop_offers = {"spark"}
assert(not Game.buy(cap, 1), "buying is refused when the deck is full")
equal(#cap.coins, Game.DECK_MAX, "deck unchanged")
equal(cap.player.gold, gold_before, "no gold spent")
equal(cap.shop_offers[1], "spark", "the offer stays")
assert(Game.remove(cap, cap.coins[1].uid), "removing a coin makes room")
assert(Game.buy(cap, 1), "and now the purchase works")
equal(#cap.coins, Game.DECK_MAX)
assert(Game.leave_shop(cap))
cap.encounter.quota = 10000
cap.player.gold = 0 -- no gold: no exchange
cap.player.energy = 99
equal(Game.coins_left(cap), Game.DECK_MAX, "the level lasts as long as the stack")
local seen = {}
for i = 1, Game.DECK_MAX do
  cap.player.energy = 99
  assert(Game.flip(cap))
  assert(not seen[cap.pending.uid], "each deck coin is played once; no reshuffle")
  seen[cap.pending.uid] = true
  assert(Game.resolve(cap))
  equal(Game.coins_left(cap), Game.DECK_MAX - i)
end
equal(cap.phase, "GAME_OVER", "the stack ran out with the quota unmet and no gold for an exchange")

-- a lost level: one coin, quota out of reach
local loss = Game.new(2)
loss.encounter.quota = 10000
loss.player.gold = 0
assert(Game.flip(loss) and Game.resolve(loss))
equal(loss.phase, "GAME_OVER", "out of coins loses")

-- Lucky: Heads sends the coin back into the pile (extra draw), at most RETURN_CAP times per level
local lucky = Game.new(9)
lucky.coins[1].id = "lucky"
lucky.encounter.quota = 10000
lucky.player.gold = 0
lucky.player.energy = 100
local plays = 0
while lucky.dealt do
  plays = plays + 1
  assert(plays < 20, "must terminate")
  lucky.dealt.probability = 1
  assert(Game.flip(lucky))
  assert(Game.resolve(lucky))
end
equal(plays, 1 + Game.RETURN_CAP, "one coin, played once plus RETURN_CAP returns")
equal(lucky.encounter.returned, Game.RETURN_CAP)
equal(lucky.phase, "GAME_OVER")

local boss = Game.new(4)
boss.encounter.boss = true
boss.encounter.flips = 4
assert(Game.flip(boss))
assert(Game.force(boss, "Heads"))
local boss_hp = boss.encounter.quota
assert(Game.resolve(boss))
equal(boss.last_result.final, "Tails", "fifth boss draw inverts")
equal(boss.encounter.quota, boss_hp, "inverted Sword Heads scores nothing")

-- raw roll is forced by setting the odds to 0 or 1, so relics and the boss rule still apply
local function play(game, side)
  game.dealt.probability = side == "Heads" and 1 or 0
  assert(Game.flip(game))
  assert(Game.resolve(game))
end

local penny = Game.new(5)
Game.add_relic(penny, "penny")
penny.encounter.quota = 1000
penny.reshuffle = true
play(penny, "Tails")
equal(penny.last_result.final, "Heads", "penny converts first Tails")
play(penny, "Tails")
equal(penny.last_result.final, "Tails", "penny only once per encounter")

local clock = Game.new(6)
Game.add_relic(clock, "clock")
clock.encounter.quota = 1000
clock.reshuffle = true
clock.encounter.flips = 9
play(clock, "Tails")
equal(clock.last_result.final, "Heads", "clock makes 10th flip Heads")

local magnet = Game.new(8)
Game.add_relic(magnet, "magnet")
magnet.encounter.quota = 1000
magnet.reshuffle = true
for _ = 1, 3 do play(magnet, "Heads") end
assert(math.abs(magnet.encounter.magnet - .05) < 1e-9, "magnet after 3 Heads")
play(magnet, "Tails")
equal(magnet.encounter.streak, 0, "Tails resets streak")
assert(Game.probability(magnet, magnet.coins[1]) > Game.catalog()[magnet.coins[1].id].probability, "magnet raises odds")

local shop = Game.new(9)
shop.phase = "SHOP"
shop.shop_relic = "magnet"
shop.player.gold = 25
assert(Game.buy_relic(shop))
equal(shop.relics[1], "magnet", "relic bought")
assert(not Game.buy_relic(shop), "one relic per shop slot")

-- discarding is free, never takes the last usable coin, and discarded coins stay out for the level
local d = Game.new(11)
equal(Game.discard(d), 0, "cannot discard the only coin")
local d2 = Game.new(12)
d2.coins[2] = {uid = 99, id = "copper", bonus = 0}
d2.encounter.pile = {}
d2.encounter.queue = {99, d2.coins[1].uid}
d2.encounter.discarded = {}
d2.encounter.quota = 1000
d2.dealt = {uid = 99, probability = .5}
local energy = d2.player.energy
equal(Game.discard(d2), 1)
equal(d2.player.energy, energy, "discarding costs nothing")
equal(d2.dealt.uid, d2.coins[1].uid, "next coin dealt")
equal(Game.discard(d2), 0, "cannot discard last remaining coin")
d2.reshuffle = true
assert(Game.flip(d2) and Game.resolve(d2))
equal(d2.dealt.uid, d2.coins[1].uid, "discarded coin never returns")

for id, r in pairs(real) do chars[id].starter, chars[id].deck, chars[id].pool, chars[id].locked = r[1], r[2], r[3], r[4] end

-- real starter sets obey the rarity limits; Blade and Trader grow from three coins
for id, expected in pairs({blade = 3, seer = 5, trader = 3}) do
  equal(#real[id][2], expected, id .. " default deck size")
  local counts = {}
  for _, coin_id in ipairs(real[id][2]) do
    local rarity = Game.catalog()[coin_id].rarity
    counts[rarity] = (counts[rarity] or 0) + 1
    assert(counts[rarity] <= Profile.rarity_limit(coin_id), id .. " default rarity limit")
  end
end
for _, id in ipairs({"blade", "seer", "trader"}) do
  local specials = 0
  for _, coin_id in ipairs(real[id][2]) do if coin_id ~= "normal" then specials = specials + 1 end end
  assert(specials >= 1, id .. " starts with at least one coin of its own")
end
local blade = Game.new(3, "blade")
equal(#blade.coins, 3)
blade.encounter.quota = 1
assert(Game.flip(blade))
blade.pending.result = "Heads"
assert(Game.resolve(blade))
assert(Game.end_level(blade))
equal(blade.phase, "SHOP", "win opens the shop")
equal(blade.cleared, 1)
equal(#blade.shop_offers, 4, "four coin offers")
local seen_offer = {}
for _, id in ipairs(blade.shop_offers) do
  assert(not seen_offer[id], "offers are distinct")
  seen_offer[id] = true
  assert(Game.catalog()[id], "offer is a real coin: " .. id)
end
-- every coin of the character, locked ones included, can show up in the shop
local blade_all = {}
for _, id in ipairs(Game.characters().blade.pool) do blade_all[id] = true end
for _, entry in ipairs(Game.characters().blade.locked) do blade_all[entry[1]] = true end
local appeared, only_listed = {}, true
for seed = 1, 120 do
  local g = Game.new(seed, "blade")
  g.phase = "SHOP"
  g.player.gold = 1000
  g.reroll_cost = 0
  Game.reroll_shop(g)
  for _, id in ipairs(g.shop_offers) do
    appeared[id] = true
    if not blade_all[id] then only_listed = false end
  end
end
assert(only_listed, "offers come from the character's own coins")
assert(appeared.hammer and appeared.sword, "locked and starting coins both appear")

-- buying a coin records it as purchased (the UI unlocks locked ones for good)
local shopper = Game.new(5, "blade", {"blood"}, {"normal", "normal", "normal", "blood", "blood"})
shopper.phase = "SHOP"
shopper.shop_offers = {"hammer"}
shopper.player.gold = 60
assert(not Game.buy(shopper, 1), "a full deck must buy a slot first")
assert(Game.buy_slot(shopper))
assert(Game.buy(shopper, 1), "now there is a free slot")
equal(#shopper.coins, 6)
assert(shopper.purchased.hammer, "purchase recorded")

-- reroll gets 2 gold dearer each time within one visit and resets in the next shop
local rr = Game.new(6, "blade")
rr.phase = "SHOP"
rr.reroll_cost = 4
rr.player.gold = 100
assert(Game.reroll_shop(rr))
equal(rr.player.gold, 96)
assert(Game.reroll_shop(rr))
equal(rr.player.gold, 90, "second reroll costs 6")
equal(rr.reroll_cost, 8)
rr.player.gold = 5
assert(not Game.reroll_shop(rr), "cannot afford")

equal(Game.run_tokens(blade), 1, "one token per level cleared")
blade.phase = "VICTORY"
equal(Game.run_tokens(blade), 4, "boss bonus")

-- a penalty (negative effect) raises the quota instead of hurting a player
local pen = Game.new(21, "blade")
pen.encounter.quota = 10
pen.encounter.max_quota = 10
Game.apply_effect(pen, nil, {type = "penalty", amount = 4})
equal(pen.encounter.quota, 14, "penalty raises the remaining quota")
equal(pen.encounter.max_quota, 14, "and the total")
equal(pen.player.hp, nil, "player has no HP")

-- the side decided at flip time is the side that counts (what the UI animates is what is scored)
local fate = Game.new(31, "blade")
Game.add_relic(fate, "penny")
fate.encounter.quota = 1000
fate.dealt.probability = 0
assert(Game.flip(fate))
equal(fate.pending.raw, "Tails")
equal(fate.pending.result, "Heads", "Lucky Penny already applied when the coin is flipped")
assert(Game.resolve(fate))
equal(fate.last_result.final, "Heads", "and it is scored as the same side")

local house = Game.new(32, "blade")
house.encounter.boss = true
house.encounter.flips = 4
house.encounter.quota = 1000
house.dealt.probability = 1
assert(Game.flip(house))
equal(house.pending.raw, "Heads")
equal(house.pending.result, "Tails", "boss inversion already applied when the coin is flipped")
assert(Game.resolve(house))
equal(house.last_result.final, "Tails")

-- clearing the quota does not end the level: keep flipping for extra gold, open the shop when you like
local k = Game.new(41, "blade")
equal(Game.end_level(k), false, "cannot leave before the quota is met")
for _, c in ipairs(k.coins) do c.id = "sword" end -- heads = 3 points
k.encounter.quota, k.encounter.max_quota = 2, 2
local gold = k.player.gold
k.dealt.probability = 1
assert(Game.flip(k) and Game.resolve(k))
equal(k.phase, "ENCOUNTER", "level stays open after clearing")
assert(k.encounter.cleared)
equal(k.cleared, 1, "counts as cleared right away")
equal(k.player.gold, gold + Game.route[1].payout, "level payout, with only 1 extra point")
k.dealt.probability = 1
assert(Game.flip(k) and Game.resolve(k))
equal(k.player.gold, gold + Game.route[1].payout + 2, "4 extra points in total pay 2 gold")
equal(k.cleared, 1, "cleared is counted once")
local before = k.player.gold
assert(Game.end_level(k))
equal(k.phase, "SHOP")
equal(k.player.gold, before, "ending pays nothing more")

-- the stack running dry after the quota is met goes to the shop (not game over)
local d3 = Game.new(42, "blade")
for _, c in ipairs(d3.coins) do c.id = "sword" end
d3.encounter.quota, d3.encounter.max_quota = 2, 2
d3.player.gold = 0
local flips_made = 0
while d3.phase == "ENCOUNTER" do
  d3.dealt.probability = 1
  assert(Game.flip(d3) and Game.resolve(d3))
  d3.player.gold = 0 -- spend the level payout: no gold left for an exchange
  flips_made = flips_made + 1
  assert(flips_made <= #d3.coins, "terminates")
end
equal(flips_made, #d3.coins, "every coin of the stack was played")
equal(d3.phase, "SHOP", "last coin played after clearing -> shop")
assert(d3.encounter.cleared)

-- the boss ends the run the moment its quota is met
local bossy = Game.new(43, "blade")
bossy.encounter.boss = true
bossy.encounter.flips = 0
for _, c in ipairs(bossy.coins) do c.id = "sword" end
bossy.encounter.quota, bossy.encounter.max_quota = 2, 2
bossy.dealt.probability = 1
assert(Game.flip(bossy))
equal(bossy.pending.result, "Heads")
assert(Game.resolve(bossy))
equal(bossy.phase, "VICTORY")

-- quotas scale with the number of coins in the deck (a level lasts as long as the stack)
equal(Game.quota_for(1, 5), 4)
equal(Game.quota_for(1, 10), 7)
equal(Game.quota_for(4, 10), 45)
equal(Game.quota_for(2, 1), 1, "never below 1")
equal(Game.new(51, "blade").encounter.quota, Game.quota_for(1, 3), "a level's quota comes from the deck size at level start")

print("game tests passed")
