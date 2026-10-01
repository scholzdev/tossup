local Profile = require("src.profile")
local characters = require("content.characters")
local blade_deck = table.concat(characters.blade.deck, ",")

local function equal(a, b, message)
  assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b))
end

local p = Profile.new()
p.tokens = 2
assert(not Profile.is_unlocked(p, "blade", "hammer"))
assert(Profile.grant(p, "blade", "hammer"), "buying a locked coin in the shop unlocks it")
assert(Profile.is_unlocked(p, "blade", "hammer"))
assert(not Profile.grant(p, "blade", "hammer"), "only newly unlocked once")
assert(not Profile.grant(p, "blade", "sword"), "sword is in the starting pool, not a locked coin")
assert(not Profile.is_unlocked(p, "seer", "hammer"), "unlocks are per character")
equal(#Profile.unlocked_list(p, "blade"), 1)

local copy = Profile.decode(Profile.encode(p))
equal(copy.tokens, 2)
assert(Profile.is_unlocked(copy, "blade", "hammer"), "survives save and load")

equal(Profile.decode("garbage {").tokens, 0, "bad file gives a fresh profile")
equal(Profile.decode(nil).tokens, 0)
equal(Profile.decode("return 5").tokens, 0)

-- collection and options survive save/load; old save files get defaults
assert(Profile.collect(p, "sword"))
assert(not Profile.collect(p, "sword"), "already collected")
p.options.fast_flip = true
local again = Profile.decode(Profile.encode(p))
assert(again.collected.sword, "collection saved")
equal(again.options.fast_flip, true, "option saved")
equal(again.options.screen_shake, true, "default option kept")
local old = Profile.decode("return {tokens = 3, unlocked = {}}")
equal(old.tokens, 3)
equal(old.options.screen_shake, true, "old save gets default options")
assert(next(old.collected) == nil)

-- characters unlock in order by winning runs; the endless record keeps the best
local u = Profile.new()
assert(Profile.character_unlocked(u, "blade"), "the first character is always open")
assert(not Profile.character_unlocked(u, "seer") and not Profile.character_unlocked(u, "trader"))
equal(Profile.required_for("seer"), "blade")
equal(Profile.required_for("blade"), nil)
equal(Profile.record_win(u, "blade"), "seer", "winning with the Blade unlocks the Seer")
assert(Profile.character_unlocked(u, "seer") and not Profile.character_unlocked(u, "trader"))
equal(Profile.record_win(u, "blade"), nil, "only the first win unlocks")
equal(Profile.record_win(u, "seer"), "trader")
assert(Profile.record_endless(u, "blade", 3) and not Profile.record_endless(u, "blade", 2) and Profile.record_endless(u, "blade", 5))
assert(not Profile.record_endless(u, "seer", 0), "no endless levels, no record")
local u2 = Profile.decode(Profile.encode(u))
assert(Profile.character_unlocked(u2, "trader"), "unlocks survive a save")
equal(u2.best_endless.blade, 5)
assert(next(Profile.decode("return {tokens = 1, unlocked = {}}").wins) == nil, "old saves have no wins")

-- coin sets: three per character, set 1 is the default deck; edits, active set and copy limits
local sp = Profile.new()
local sets = Profile.sets(sp, "blade")
equal(#sets, 3)
equal(table.concat(sets[1].coins, ","), blade_deck, "set 1 is the default deck")
equal(#sets[2].coins, 0, "other sets start empty")
for _ = 1, 3 do assert(Profile.add_to_set(sp, "blade", 2, "sword", 10, 3)) end
assert(not Profile.add_to_set(sp, "blade", 2, "sword", 10, 3), "a fourth copy is refused")
for _ = 1, 7 do assert(Profile.add_to_set(sp, "blade", 2, "normal", 10, 3), "normal has no copy limit") end
assert(not Profile.add_to_set(sp, "blade", 2, "normal", 10, 3), "set is full")
assert(not Profile.add_to_set(sp, "blade", 3, "hammer", 10, 3), "locked coins cannot be added")
assert(Profile.remove_from_set(sp, "blade", 2, 1))
equal(#Profile.sets(sp, "blade")[2].coins, 9)
local all_normal = Profile.new()
Profile.sets(all_normal, "blade")[1].coins = {}
for _ = 1, 10 do assert(Profile.add_to_set(all_normal, "blade", 1, "normal", 10, 3), "ten Normal coins are fine") end
Profile.set_active(sp, "blade", 2)
equal(Profile.active(sp, "blade"), 2)
equal(#Profile.loadout(sp, "blade", 10), 9, "the active set is what a run starts with")
Profile.set_active(sp, "blade", 9)
equal(Profile.active(sp, "blade"), 2, "invalid set index ignored")
local saved = Profile.decode(Profile.encode(sp))
equal(#saved.sets.blade[2].coins, Profile.SET_SIZE, "sets survive a save; older, longer sets are cut to SET_SIZE")
equal(Profile.SET_SIZE, require("src.game").START_MAX, "a set holds exactly the starting slots")
equal(saved.sets.blade[2].name, "SET 2")
equal(saved.active_set.blade, 2)

-- an old save with a single loadout becomes set 1
local legacy = Profile.decode('return {tokens = 1, unlocked = {}, loadouts = {blade = {"sword", "normal"}}}')
equal(table.concat(Profile.sets(legacy, "blade")[1].coins, ","), "sword,normal")
equal(legacy.loadouts, nil)

-- Profile.loadout: empty set falls back to the default deck; locked coins are filtered out
local fresh2 = Profile.new()
Profile.set_active(fresh2, "blade", 3)
equal(table.concat(Profile.loadout(fresh2, "blade", 10), ","), blade_deck, "empty set falls back")
Profile.sets(fresh2, "blade")[3].coins = {"hammer", "sword"}
equal(table.concat(Profile.loadout(fresh2, "blade", 10), ","), "sword", "locked coin filtered out")

-- a save with too many copies (from before the limit existed) still yields a valid loadout
local over = Profile.new()
Profile.sets(over, "blade")[1].coins = {"sword", "sword", "sword", "normal", "normal", "normal", "dagger"}
Profile.sets(over, "blade")[1].coins = {"sword", "sword", "sword", "sword", "normal", "normal", "dagger"}
equal(table.concat(Profile.loadout(over, "blade", 10, 3), ","), "sword,sword,sword,normal,normal,dagger", "fourth sword dropped")

print("profile tests passed")
