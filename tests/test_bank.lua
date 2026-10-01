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
  assert(Game.flip(g) and Game.resolve(g))
end
local count = 0
for _ in pairs(seen) do count = count + 1 end
equal(count, 8, "all eight coins were played")

-- manual mulligan: look at five, discard for energy, keep the rest
local m = Game.new(2, "big", nil, nil, true)
assert(m.mulligan, "mulligan open")
equal(#m.mulligan.hand, 5)
equal(m.dealt, nil, "nothing dealt during the mulligan")
assert(not Game.flip(m), "cannot flip before the mulligan is done")
local first, second = m.mulligan.hand[1], m.mulligan.hand[2]
assert(Game.mulligan_discard(m, first))
equal(m.player.energy, 2, "discard costs one energy")
equal(#m.mulligan.hand, 4)
assert(not Game.mulligan_discard(m, first), "already gone")
assert(Game.mulligan_discard(m, second))
assert(Game.mulligan_discard(m, m.mulligan.hand[1]))
equal(m.player.energy, 0)
assert(not Game.mulligan_discard(m, m.mulligan.hand[1]), "no energy left")
local kept = {table.unpack(m.mulligan.hand)}
assert(Game.mulligan_done(m))
equal(m.mulligan, nil)
equal(m.dealt.uid, kept[1], "first kept coin is dealt")
equal(#m.encounter.queue, 3, "bank refilled to three")
m.encounter.quota, m.encounter.max_quota, m.encounter.draws = 1e9, 1e9, 1e9
for _ = 1, 40 do
  assert(m.encounter.discarded[m.dealt.uid] == nil, "discarded coin never comes back")
  assert(Game.flip(m) and Game.resolve(m))
end

-- cannot throw away the whole hand
local solo = Game.new(3, "big", nil, nil, true)
solo.player.energy = 99
for _ = 1, 10 do Game.mulligan_discard(solo, solo.mulligan.hand[1]) end
equal(#solo.mulligan.hand, 1, "at least one coin is kept")

-- next level starts with a fresh mulligan
local lvl = Game.new(4, "big", nil, nil, true)
Game.mulligan_done(lvl)
lvl.encounter.quota = 0
lvl.phase = "SHOP"
lvl.shop_offers = {}
assert(Game.leave_shop(lvl))
assert(lvl.mulligan, "mulligan again on the next level")
equal(lvl.player.energy, lvl.player.max_energy, "energy refilled")

-- loadouts: at most START_MAX coins, only coins the character can use
local ok = Game.new(5, "blade", nil, {"normal", "sword", "dagger", "normal"})
equal(#ok.coins, 4)
equal(ok.coins[2].id, "sword")
assert(not pcall(Game.new, 5, "blade", nil, {"normal", "normal", "normal", "normal", "normal", "normal"}), "too many")
assert(not pcall(Game.new, 5, "blade", nil, {}), "empty")
assert(not pcall(Game.new, 5, "blade", nil, {"hammer"}), "locked coin")
assert(pcall(Game.new, 5, "blade", {"hammer"}, {"hammer", "normal"}), "unlocked coin is allowed")

print("bank tests passed")
