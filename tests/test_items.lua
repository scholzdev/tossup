local Game = require("src.game")
local Signal = require("src.signal")
-- these tests want a small deck so the draw pile starts empty
Game.characters().blade.deck = {"normal", "normal", "normal"}

local function equal(a, b, message)
  assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b))
end

-- run with a huge enemy so nothing ends the level, three coins, items in hand
local function fresh(items, seed)
  local game = Game.new(seed or 1, "blade")
  game.encounter.quota = 1000
  game.encounter.draws = 1000
  game.items = items
  return game
end

local function flip_resolve(game)
  assert(Game.flip(game))
  local result = game.pending.result
  assert(Game.resolve(game))
  return result
end

-- force heads / tails: every seed, the forced side always wins; consumed
for seed = 1, 30 do
  local g = fresh({"force_heads", "force_tails"}, seed)
  assert(Game.use_item(g, 1))
  assert(Game.flip(g))
  equal(g.pending.result, "Heads", "forced Heads")
  assert(Game.resolve(g))
  equal(#g.items, 1, "item consumed")
  assert(Game.use_item(g, 1))
  assert(Game.flip(g))
  equal(g.pending.result, "Tails", "forced Tails")
  Game.resolve(g)
end

-- one-shot: the flip after a forced flip is not forced
local g = fresh({"force_heads"}, 3)
assert(Game.use_item(g, 1))
flip_resolve(g)
local tails = false
for _ = 1, 30 do if flip_resolve(g) == "Tails" then tails = true end end
assert(tails, "force lasts one flip only")

-- cannot use an item while a coin is mid-flip, or with nothing in the slot
local m = fresh({"weighted"})
assert(Game.flip(m))
assert(not Game.use_item(m, 1), "no items after the flip")
Game.resolve(m)
assert(not Game.use_item(m, 2), "empty slot")

-- weighted raises the dealt coin's odds for one flip
local w = fresh({"weighted"})
local before = w.dealt.probability
assert(Game.use_item(w, 1))
assert(math.abs(w.dealt.probability - (before + .25)) < 1e-9, "weighted +25%")
flip_resolve(w)
assert(w.dealt.probability <= before + 1e-9, "next coin not weighted")

-- double down doubles the next coin's effect, then is gone
local d = fresh({"double_down"})
for _, c in ipairs(d.coins) do c.id = "sword" end -- heads = 5 points
d.dealt.probability = 1
assert(Game.use_item(d, 1))
local hp = d.encounter.quota
flip_resolve(d)
equal(hp - d.encounter.quota, 10, "points doubled")
d.dealt.probability = 1
hp = d.encounter.quota
flip_resolve(d)
equal(hp - d.encounter.quota, 5, "only once")

-- swap: free discard of the dealt coin
local s = fresh({"swap"})
local energy = s.player.energy
local first = s.dealt.uid
assert(Game.use_item(s, 1))
equal(s.player.energy, energy, "swap is free")
assert(s.dealt.uid ~= first, "new coin dealt")
assert(s.encounter.discarded[first], "old coin discarded for the level")

-- peek shows the top of the draw pile (what refills the bank next) without consuming it
local p = fresh({"peek"})
for uid = 70, 72 do p.coins[#p.coins + 1] = {uid = uid, id = "normal", bonus = 0} end
p.encounter.pile = {70, 71, 72}
assert(Game.use_item(p, 1))
equal(p.peek[1], 70, "peek shows the next draw")
equal(p.peek[2], 71)
flip_resolve(p)
equal(p.peek, nil, "peek cleared on the next deal")
equal(p.encounter.queue[Game.VISIBLE], 70, "the peeked coin refilled the bank")

-- extra draw is capped at 3 per level and refuses (is not consumed) past the cap
local e = fresh({"extra_draw", "extra_draw", "extra_draw", "extra_draw"})
local draws = e.encounter.draws
for _ = 1, 3 do assert(Game.use_item(e, 1)) end
equal(e.encounter.draws, draws + 3)
assert(not Game.use_item(e, 1), "cap reached")
equal(#e.items, 1, "refused item is kept")

-- shop: buy with gold, slot limit, cost
local shop = Game.new(2, "blade")
shop.phase = "SHOP"
shop.shop_items = {"swap", "peek", "weighted", "swap"}
shop.player.gold = 100
assert(Game.buy_item(shop, 1) and Game.buy_item(shop, 2) and Game.buy_item(shop, 3))
assert(not Game.buy_item(shop, 4), "only three item slots")
equal(#shop.items, 3)
equal(shop.player.gold, 100 - 8 - 6 - 8, "item prices")
assert(not Game.buy_item(shop, 1), "already bought")

-- armed items do not leak into the next game
local leak = fresh({"force_tails"})
assert(Game.use_item(leak, 1))
local after = fresh({})
assert(Game.flip(after))
local heads = false
for _ = 1, 40 do
  if after.pending and after.pending.result == "Heads" then heads = true end
  Game.resolve(after)
  if after.dealt then Game.flip(after) end
end
assert(heads, "new game is not forced by an old game's item")

-- relics run on the hook bus and follow the game that owns them
local r1 = fresh({})
Game.add_relic(r1, "clock")
local r2 = fresh({}) -- new game: r1's relic must not affect it
r2.encounter.flips = 9
assert(Game.flip(r2))
r2.pending.result = "Tails"
Game.resolve(r2)
equal(r2.last_result.final, "Tails", "relic of another game is unbound")

print("item tests passed")
