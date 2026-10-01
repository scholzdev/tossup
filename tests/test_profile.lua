local Profile = require("src.profile")

local function equal(a, b, message)
  assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b))
end

local p = Profile.new()
assert(not Profile.unlock(p, "blade", "hammer"), "not enough tokens")
p.tokens = 5
assert(Profile.unlock(p, "blade", "hammer"))
equal(p.tokens, 2, "tokens spent")
assert(Profile.is_unlocked(p, "blade", "hammer"))
assert(not Profile.unlock(p, "blade", "hammer"), "cannot buy twice")
assert(not Profile.unlock(p, "blade", "sword"), "sword is not a locked coin")
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

-- coin sets: three per character, set 1 is the default deck; edits, active set and copy limits
local sp = Profile.new()
local sets = Profile.sets(sp, "blade")
equal(#sets, 3)
equal(table.concat(sets[1].coins, ","), "normal,normal,normal", "set 1 is the default deck")
equal(#sets[2].coins, 0, "other sets start empty")
assert(Profile.add_to_set(sp, "blade", 2, "sword", 10, 2))
assert(Profile.add_to_set(sp, "blade", 2, "sword", 10, 2))
assert(not Profile.add_to_set(sp, "blade", 2, "sword", 10, 2), "copy limit")
for _ = 1, 8 do assert(Profile.add_to_set(sp, "blade", 2, "normal", 10, 2), "normal has no copy limit") end
assert(not Profile.add_to_set(sp, "blade", 2, "normal", 10, 2), "set is full")
assert(not Profile.add_to_set(sp, "blade", 3, "hammer", 10, 2), "locked coins cannot be added")
assert(Profile.remove_from_set(sp, "blade", 2, 1))
equal(#Profile.sets(sp, "blade")[2].coins, 9)
Profile.set_active(sp, "blade", 2)
equal(Profile.active(sp, "blade"), 2)
equal(#Profile.loadout(sp, "blade", 10), 9, "the active set is what a run starts with")
Profile.set_active(sp, "blade", 9)
equal(Profile.active(sp, "blade"), 2, "invalid set index ignored")
local saved = Profile.decode(Profile.encode(sp))
equal(#saved.sets.blade[2].coins, 9, "sets survive a save")
equal(saved.sets.blade[2].name, "SET 2")
equal(saved.active_set.blade, 2)

-- an old save with a single loadout becomes set 1
local legacy = Profile.decode('return {tokens = 1, unlocked = {}, loadouts = {blade = {"sword", "normal"}}}')
equal(table.concat(Profile.sets(legacy, "blade")[1].coins, ","), "sword,normal")
equal(legacy.loadouts, nil)

-- Profile.loadout: empty set falls back to the default deck; locked coins are filtered out
local fresh2 = Profile.new()
Profile.set_active(fresh2, "blade", 3)
equal(table.concat(Profile.loadout(fresh2, "blade", 10), ","), "normal,normal,normal", "empty set falls back")
Profile.sets(fresh2, "blade")[3].coins = {"hammer", "sword"}
equal(table.concat(Profile.loadout(fresh2, "blade", 10), ","), "sword", "locked coin filtered out")

-- a save with too many copies (from before the limit existed) still yields a valid loadout
local over = Profile.new()
Profile.sets(over, "blade")[1].coins = {"sword", "sword", "sword", "normal", "normal", "normal", "dagger"}
equal(table.concat(Profile.loadout(over, "blade", 10, 2), ","), "sword,sword,normal,normal,normal,dagger", "third sword dropped")

print("profile tests passed")
