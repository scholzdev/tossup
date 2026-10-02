local Game = require("src.game")
Game.use_modifiers = false -- modifiers have their own tests (test_modifiers.lua)

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
g.player.gold = Game.EXCHANGE_BASE -- this fixture expects an exchange to be affordable after the stack empties
equal(#g.coins, 8)
equal(#g.encounter.queue, 5, "opening hand")
equal(#g.encounter.pile, 3, "rest of the deck is the draw pile")
equal(g.dealt.uid, g.encounter.queue[1], "front of the bank is the dealt coin")
g.encounter.quota, g.encounter.max_quota = 1e9, 1e9

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

-- every coin is played exactly once (no reshuffle); then the stack is empty and the level ends
local seen = {}
local played = 3 -- three coins were already flipped above
for uid in pairs({}) do seen[uid] = true end
while g.dealt do
  assert(not seen[g.dealt.uid], "no coin is played twice")
  seen[g.dealt.uid] = true
  g.player.energy = 99
  assert(Game.flip(g) and Game.resolve(g))
  played = played + 1
end
equal(played, 8, "all eight coins were played once")
equal(Game.coins_left(g), 0)
equal(g.phase, "ENCOUNTER", "stack empty, quota unmet, gold for an exchange: the player decides")
assert(g.exchange_open)

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
m.encounter.quota, m.encounter.max_quota = 1e9, 1e9
m.reshuffle = true
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
lv.encounter.quota, lv.encounter.max_quota = 1e9, 1e9
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

-- coin sets: at most START_MAX coins, with rarity-based copy limits, only usable coins
local ok = Game.new(5, "blade", nil, {"normal", "sword", "dagger"})
equal(#ok.coins, 3)
equal(ok.coins[2].id, "sword")
local full = {"normal", "normal", "normal", "hammer", "hammer"}
equal(#Game.new(5, "blade", {"hammer"}, full).coins, Game.START_MAX, "a full legal set is fine")
full[#full + 1] = "normal"
assert(not pcall(Game.new, 5, "blade", {"hammer"}, full), "too many coins")
assert(not pcall(Game.new, 5, "blade", nil, {"normal", "normal", "normal", "normal"}), "four Normal coins are refused")
assert(not pcall(Game.new, 5, "blade", nil, {}), "empty")
assert(not pcall(Game.new, 5, "blade", nil, {"hammer"}), "locked coin")
assert(pcall(Game.new, 5, "blade", {"hammer"}, {"hammer", "normal"}), "unlocked coin is allowed")
assert(pcall(Game.new, 5, "blade", nil, {"sword", "sword", "sword"}), "three common copies are fine")
assert(not pcall(Game.new, 5, "blade", nil, {"sword", "sword", "sword", "sword"}), "four copies are refused")
assert(not pcall(Game.new, 5, "blade", nil, {"normal", "sword", "dagger", "normal"}), "four commons of different types refused")
assert(pcall(Game.new, 5, "blade", {"hammer"}, {"hammer", "hammer"}), "two uncommon copies are fine")
assert(not pcall(Game.new, 5, "blade", {"hammer"}, {"hammer", "hammer", "hammer"}), "third uncommon refused")
assert(not pcall(Game.new, 5, "blade", {"cursed"}, {"cursed", "cursed"}), "second rare refused")
assert(not pcall(Game.new, 5, "seer", {"mimic"}, {"mimic", "mimic"}), "second epic refused")

-- energy cost: a coin you cannot pay for cannot be flipped (discard it instead); the last coin always flips
Game.characters().pricey = {name = "P", description = "", starter = "normal", deck = {"hammer", "normal", "normal"},
  pool = {"normal", "hammer"}, locked = {}}
local px = Game.new(7, "pricey")
px.encounter.queue = {px.coins[1].uid, px.coins[2].uid, px.coins[3].uid}
Game.discard(px, {}) -- no-op; refresh nothing
px.dealt = {uid = px.coins[1].uid, probability = .5}
px.encounter.quota, px.encounter.max_quota = 1e9, 1e9
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

-- exchange: with an empty stack, pay gold to get played coins back (price rises per exchange)
Game.characters().exch = {name = "E", description = "", starter = "normal",
  deck = {"normal", "normal", "normal", "normal", "sword", "sword", "dagger"},
  pool = {"normal", "sword", "dagger"}, locked = {}}
local function play_out(g)
  while g.dealt do
    g.player.energy = 99
    assert(Game.flip(g) and Game.resolve(g))
  end
end
local ex = Game.new(11, "exch")
ex.encounter.quota, ex.encounter.max_quota = 1e9, 1e9
ex.player.gold = 100
equal(Game.can_exchange(ex), false, "not while coins remain")
play_out(ex)
equal(Game.coins_left(ex), 0)
equal(ex.phase, "ENCOUNTER", "the level is not lost yet: an exchange is possible")
assert(ex.exchange_open, "the UI is asked to offer the exchange")
equal(Game.exchange_cost(ex), Game.EXCHANGE_BASE, "first exchange costs the base price")
assert(Game.can_exchange(ex))
assert(Game.exchange(ex))
equal(ex.player.gold, 100 - Game.EXCHANGE_BASE, "gold was paid")
equal(Game.coins_left(ex), Game.EXCHANGE_GAIN, "three played coins came back")
assert(ex.dealt, "play continues")
equal(ex.exchange_open, false)
equal(Game.exchange_cost(ex), Game.EXCHANGE_BASE + Game.EXCHANGE_STEP, "the next exchange costs more")
equal(ex.encounter.discards, 0, "nothing is burned")
play_out(ex)
assert(ex.exchange_open, "can exchange again")
local gold_before = ex.player.gold
assert(Game.exchange(ex))
equal(ex.player.gold, gold_before - (Game.EXCHANGE_BASE + Game.EXCHANGE_STEP))
play_out(ex)
assert(Game.give_up(ex), "or give up instead")
equal(ex.phase, "GAME_OVER")

-- at most EXCHANGE_MAX exchanges per level: the next empty stack loses the level even with plenty of gold
local lim = Game.new(21, "exch")
lim.encounter.quota, lim.encounter.max_quota = 1e9, 1e9
lim.player.gold = 1000
play_out(lim)
for n = 1, Game.EXCHANGE_MAX do
  assert(Game.can_exchange(lim), "exchange " .. n .. " is allowed")
  assert(Game.exchange(lim))
  play_out(lim)
end
equal(Game.can_exchange(lim), false, "no fourth exchange")
equal(lim.phase, "GAME_OVER", "the stack is empty and the exchanges are used up: level lost")
equal(lim.lost_why, "out of coins, and all exchanges are used.")

-- discarded coins never come back
local exd = Game.new(15, "exch")
exd.encounter.quota, exd.encounter.max_quota = 1e9, 1e9
exd.player.gold = 100
local dropped = exd.encounter.queue[2]
assert(Game.discard(exd, {dropped}) == 1)
play_out(exd)
assert(Game.exchange(exd))
for _, uid in ipairs(exd.encounter.queue) do assert(uid ~= dropped, "a discarded coin does not return") end

-- not enough gold: the level is lost the moment the stack is empty
local poor = Game.new(12, "exch")
poor.encounter.quota, poor.encounter.max_quota = 1e9, 1e9
poor.player.gold = Game.EXCHANGE_BASE - 1
play_out(poor)
equal(poor.phase, "GAME_OVER", "cannot afford an exchange: level lost")

-- a cleared level with an empty stack stays open when an exchange is affordable, otherwise goes to the shop
local ex3 = Game.new(13, "exch")
ex3.encounter.quota, ex3.encounter.max_quota = 1, 1
ex3.player.gold = 100
play_out(ex3)
assert(ex3.encounter.cleared)
equal(Game.coins_left(ex3), 0)
equal(ex3.phase, "ENCOUNTER", "stays open: an exchange is affordable")
assert(Game.end_level(ex3), "and the player can still open the shop")
equal(ex3.phase, "SHOP")
local ex5 = Game.new(16, "exch")
ex5.encounter.quota, ex5.encounter.max_quota = 1, 1
while ex5.dealt do
  ex5.player.energy = 99
  assert(Game.flip(ex5) and Game.resolve(ex5))
  ex5.player.gold = 0 -- spend the payout: no gold for an exchange
end
equal(ex5.phase, "SHOP", "cleared, stack empty, no gold for an exchange: straight to the shop")

-- give_up is refused while coins remain
local ex4 = Game.new(14, "exch")
equal(Game.give_up(ex4), false)

print("bank tests passed")
