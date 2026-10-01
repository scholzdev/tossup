-- Saving and restoring a run at its safe points (opening hand, shop): the restored game plays out exactly like the original.
local Game = require("src.game")
local Serialize = require("src.serialize")

local function equal(a, b, message) assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b)) end

-- the serializer round-trips plain data, sorts keys and skips functions
local data = {a = 1, b = {1, 2, false, "x\ny"}, c = {n = 0.5, [3] = true}, f = function() end, s = "quote\"d"}
local back = Serialize.decode(Serialize.encode(data))
equal(back.a, 1)
equal(back.b[3], false)
equal(back.b[4], "x\ny")
equal(back.c.n, 0.5)
equal(back.c[3], true)
equal(back.f, nil, "functions are not saved")
equal(back.s, "quote\"d")
equal(Serialize.decode("garbage {"), nil)
equal(Serialize.encode(data), Serialize.encode(data), "deterministic")

-- play a deterministic script on a game: flip everything in the level
local function play_out(g)
  g.reshuffle = false
  while g.dealt do
    g.player.energy = 99
    assert(Game.flip(g) and Game.resolve(g))
  end
  return g.encounter.scored, g.player.gold, #g.log
end

local function through_disk(g) return Game.restore(Serialize.decode(Serialize.encode(Game.snapshot(g)))) end

-- opening hand of level 1, with a relic that hooks into flips
local a = Game.new(77, "blade", nil, nil, false) -- keeps the hand automatically; start from a fresh level instead:
a = Game.new(77, "blade", nil, nil, true)
Game.add_relic(a, "metronome")
assert(a.mulligan, "the opening hand is open")
local restored = through_disk(a)
assert(restored, "an opening-hand snapshot restores")
equal(restored.seed, a.seed)
equal(#restored.coins, #a.coins)
equal(restored.rng_state, a.rng_state)
equal(#restored.mulligan.hand, #a.mulligan.hand)
Game.mulligan_done(a)
Game.mulligan_done(restored)
local s1, g1 = play_out(a)
local s2, g2 = play_out(restored)
equal(s1, s2, "same points after the same flips")
equal(g1, g2, "same gold")
equal(restored.rng_state, a.rng_state, "same RNG state")

-- the shop
local s = Game.new(78, "seer", nil, nil, false)
s.encounter.quota, s.encounter.max_quota = 1, 1
s.player.gold = 99
s.phase = "SHOP"
s.shop_offers = {"dagger", false, "hammer", "focus"}
s.items = {"peek"}
local shop = through_disk(s)
assert(shop and shop.phase == "SHOP")
equal(shop.shop_offers[2], false, "a sold offer stays sold")
equal(shop.shop_offers[3], "hammer")
equal(shop.items[1], "peek")
equal(shop.player.gold, 99)
assert(Game.buy_slot(shop) and Game.leave_shop(shop), "the restored shop works and leads to the next level")
equal(shop.phase, "ENCOUNTER")

-- not a safe point, or not a run: refused
local mid = Game.new(79, "blade", nil, nil, false)
equal(Game.restore(Serialize.decode(Serialize.encode(Game.snapshot(mid)))), nil, "mid-level snapshots are refused")
equal(Game.restore({}), nil)
equal(Game.restore(nil), nil)
equal(Game.restore({coins = {}, player = {}, character_id = "nobody", phase = "SHOP"}), nil)

print("save tests passed")
