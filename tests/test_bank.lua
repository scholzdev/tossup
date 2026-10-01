local Game = require("src.game")

local function equal(a, b, message)
  assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b))
end

-- a character with an 8-coin deck so the draw pile is not empty
Game.characters().big = {name = "Big", description = "", starter = "normal",
  deck = {"normal", "sword", "dagger", "hammer", "blood", "spark", "focus", "copper"},
  pool = {"normal", "sword", "dagger", "hammer"}, locked = {}}

local function visible(g) return #g.encounter.queue end

-- auto mulligan (headless default): five coins in the bank, the rest in the pile
local g = Game.new(1, "big")
equal(#g.coins, 8)
equal(#g.encounter.queue, 5, "opening hand")
equal(#g.encounter.pile, 3, "rest of the deck is the draw pile")
equal(g.dealt.uid, g.encounter.queue[1], "front of the bank is the dealt coin")
g.encounter.quota, g.encounter.max_quota, g.encounter.draws = 1e9, 1e9, 1e9

-- playing a coin removes it from the bank; the bank only refills once fewer than three are left
local front = g.dealt.uid
assert(Game.flip(g))
for _, uid in ipairs(g.encounter.queue) do assert(uid ~= front, "flipped coin left the bank") end
equal(#g.encounter.queue, 4, "no refill while three or more remain")
equal(#g.encounter.pile, 3)
assert(Game.resolve(g))
assert(g.dealt.uid ~= front)
assert(Game.flip(g) and Game.resolve(g)) -- bank 3
local pile_top = g.encounter.pile[1]
assert(Game.flip(g)) -- bank would be 2: refilled from the pile
equal(#g.encounter.queue, 3)
equal(g.encounter.queue[3], pile_top, "refilled from the top of the pile")
assert(Game.resolve(g))

-- every coin shows up before the pile reshuffles; the bank never runs empty
local seen = {}
for _ = 1, 40 do
  assert(g.dealt, "always something to play")
  seen[g.dealt.uid] = true
  g.player.energy = 99
  assert(Game.flip(g) and Game.resolve(g))
end
local count = 0
for _ in pairs(seen) do count = count + 1 end
equal(count, 8, "all eight coins were played")

-- manual mulligan: look at five, mark coins to discard (free), keep the rest
local m = Game.new(2, "big", nil, nil, true)
assert(m.mulligan, "mulligan open")
equal(#m.mulligan.hand, 5)
equal(m.dealt, nil, "nothing dealt during the mulligan")
assert(not Game.flip(m), "cannot flip before the mulligan is done")
local first, second, third = m.mulligan.hand[1], m.mulligan.hand[2], m.mulligan.hand[3]
equal(Game.mulligan_discard(m, {}), 0, "nothing marked, nothing discarded")
equal(Game.mulligan_discard(m, {first, second, third}), 3, "several coins at once")
equal(m.player.energy, 3, "discarding is free")
equal(#m.mulligan.hand, 2)
equal(Game.mulligan_discard(m, {first}), 0, "already gone")
local kept = {table.unpack(m.mulligan.hand)}
assert(Game.mulligan_done(m))
equal(m.mulligan, nil)
equal(m.dealt.uid, kept[1], "first kept coin is dealt")
equal(#m.encounter.queue, 3, "bank refilled to three")
m.encounter.quota, m.encounter.max_quota, m.encounter.draws = 1e9, 1e9, 1e9
for _ = 1, 40 do
  assert(m.encounter.discarded[m.dealt.uid] == nil, "discarded coin never comes back")
  m.player.energy = 99 -- some coins in this deck cost energy; this loop is about the bank
  assert(Game.flip(m) and Game.resolve(m))
end

-- cannot throw away the whole hand
local solo = Game.new(3, "big", nil, nil, true)
local everything = {table.unpack(solo.mulligan.hand)}
equal(Game.mulligan_discard(solo, everything), 4, "all but one")
equal(#solo.mulligan.hand, 1, "at least one coin is kept")

-- discarding from the bank mid-level: any visible coins, free, front coin is replaced
local lv = Game.new(6, "big")
lv.encounter.quota, lv.encounter.max_quota, lv.encounter.draws = 1e9, 1e9, 1e9
local q1, q2, q3 = lv.encounter.queue[1], lv.encounter.queue[2], lv.encounter.queue[3]
equal(Game.discard(lv, {}), 0, "nothing marked")
equal(Game.discard(lv, {q2}), 1, "a non-front coin")
equal(lv.dealt.uid, q1, "front coin stays dealt")
assert(lv.encounter.discarded[q2])
equal(Game.discard(lv, {q1, q3}), 2)
assert(lv.dealt.uid ~= q1 and lv.dealt.uid ~= q3, "new front coin dealt")
equal(lv.player.energy, 3, "no energy spent")
assert(Game.flip(lv) and Game.resolve(lv))

-- next level starts with a fresh mulligan
local lvl = Game.new(4, "big", nil, nil, true)
Game.mulligan_done(lvl)
lvl.encounter.quota = 0
lvl.phase = "SHOP"
lvl.shop_offers = {}
assert(Game.leave_shop(lvl))
assert(lvl.mulligan, "mulligan again on the next level")
equal(lvl.player.energy, lvl.player.max_energy, "energy refilled")

-- coin sets: at most START_MAX coins, at most MAX_COPIES of a coin (Normal is exempt), only usable coins
local ok = Game.new(5, "blade", nil, {"normal", "sword", "dagger", "normal"})
equal(#ok.coins, 4)
equal(ok.coins[2].id, "sword")
local ten = {}
for i = 1, Game.START_MAX do ten[i] = "normal" end
equal(#Game.new(5, "blade", nil, ten).coins, Game.START_MAX, "a full set of Normal coins is fine")
ten[#ten + 1] = "normal"
assert(not pcall(Game.new, 5, "blade", nil, ten), "too many coins")
assert(not pcall(Game.new, 5, "blade", nil, {}), "empty")
assert(not pcall(Game.new, 5, "blade", nil, {"hammer"}), "locked coin")
assert(pcall(Game.new, 5, "blade", {"hammer"}, {"hammer", "normal"}), "unlocked coin is allowed")
assert(pcall(Game.new, 5, "blade", nil, {"sword", "sword", "normal"}), "two copies are fine")
assert(not pcall(Game.new, 5, "blade", nil, {"sword", "sword", "sword"}), "three copies are not")

-- energy cost: a coin you cannot pay for cannot be flipped (discard it instead); the last coin always flips
Game.characters().pricey = {name = "P", description = "", starter = "normal", deck = {"hammer", "normal", "normal"},
  pool = {"normal", "hammer"}, locked = {}}
local px = Game.new(7, "pricey")
px.encounter.queue = {px.coins[1].uid, px.coins[2].uid, px.coins[3].uid}
Game.discard(px, {}) -- no-op; refresh nothing
px.dealt = {uid = px.coins[1].uid, probability = .5}
px.encounter.quota, px.encounter.max_quota, px.encounter.draws = 1e9, 1e9, 1e9
equal(Game.flip_cost(px, px.dealt.uid), 2, "Hammer costs 2")
px.player.energy = 1
assert(not Game.can_flip(px), "cannot afford it")
assert(not Game.flip(px), "flip refused")
equal(px.player.energy, 1, "nothing was charged")
equal(Game.discard(px), 1, "but discarding is free")
px.encounter.queue = {px.coins[1].uid} -- put the Hammer back as the only usable coin
px.encounter.discarded = {[px.coins[2].uid] = true, [px.coins[3].uid] = true}
px.encounter.discards = 2
px.dealt = {uid = px.coins[1].uid, probability = .5}
assert(Game.can_flip(px), "the last usable coin always flips")
assert(Game.flip(px))
equal(px.player.energy, 0, "pays what it can")
px.player.energy = 3
Game.resolve(px)
local fresh_px = Game.new(8, "pricey")
fresh_px.encounter.queue = {fresh_px.coins[1].uid, fresh_px.coins[2].uid}
fresh_px.dealt = {uid = fresh_px.coins[1].uid, probability = .5}
fresh_px.player.energy = 3
assert(Game.flip(fresh_px))
equal(fresh_px.player.energy, 1, "Hammer cost 2 energy")

print("bank tests passed")
