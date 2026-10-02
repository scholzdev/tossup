-- Stages (difficulty): profile unlocks and the rules each stage adds (content/stakes.lua).
package.path = "./?.lua;" .. package.path
local Game = require("src.game")
local Profile = require("src.profile")
local Lang = require("src.lang")
local de = require("locales.de")

local function equal(a, b, message) assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b)) end

-- profile: stage 1 at first, each win on the highest stage unlocks the next, per character
local p = Profile.new()
equal(Profile.max_stake(p, "blade"), 1)
equal(Profile.record_stake_win(p, "blade", 1), 2)
equal(Profile.record_stake_win(p, "blade", 1), nil, "a win on a lower stage unlocks nothing")
equal(Profile.record_stake_win(p, "blade", 2), 3)
equal(Profile.max_stake(p, "seer"), 1, "stages are per character")
p.stakes.blade = #Game.stakes()
equal(Profile.record_stake_win(p, "blade", #Game.stakes()), nil, "no stage beyond the last")
local old = Profile.new()
old.wins.blade = true
equal(Profile.max_stake(old, "blade"), 2, "a profile from before stages: a won character starts at stage 2")
local back = Profile.decode(Profile.encode(p))
equal(Profile.max_stake(back, "blade"), #Game.stakes())

-- every stage text has a German line
Lang.set("de")
for _, stage in ipairs(Game.stakes()) do assert(de[stage.text], "missing German text: " .. stage.text) end
Lang.set("en")

local function fresh(stake) return Game.new(3, "blade", {}, nil, true, stake) end
local base = fresh(1)
equal(base.player.gold, Game.START_GOLD)
equal(Game.rule(fresh(2), "quota_mult", 1), 1.15, "stage 2: quotas +15%")
equal(fresh(2).encounter.quota, math.floor(Game.stage(1).per_coin * #base.coins * 1.15 + .5))
equal(fresh(3).player.gold, 15, "stage 3: start with 15 gold")
equal(Game.price(fresh(4), 10), 10)
equal(Game.price(fresh(5), 10), 12, "stage 5: prices +20%")
equal(Game.price(fresh(5), 25), 30)
-- the shop charges the stage price
local shop = fresh(5)
shop.phase, shop.shop_relic, shop.player.gold = "SHOP", "magnet", 30
assert(Game.buy_relic(shop))
equal(shop.player.gold, 0, "a prize costs 30 on stage 5")
equal(Game.exchanges_left(fresh(5)), 3)
equal(Game.exchanges_left(fresh(6)), 2, "stage 6: two exchanges")
-- stage 4: the House inverts every 4th flip, so the 5th is no longer special
local boss = fresh(4)
boss.encounter.boss, boss.encounter.inverts = true, true
equal(Game.rule(boss, "boss_every", 5), 4)
equal(Game.rule(base, "boss_every", 5), 5)
-- stage 7: level 1 already has a modifier; stage 8: quotas +80% in total (replaces +15%)
assert(fresh(7).encounter.modifier, "stage 7: modifier on level 1")
assert(not fresh(6).encounter.modifier)
equal(fresh(8).encounter.quota, math.floor(Game.stage(1).per_coin * #base.coins * 1.8 + .5))
equal(fresh(99).stake, #Game.stakes(), "stage is clamped")

print("stakes tests passed")
