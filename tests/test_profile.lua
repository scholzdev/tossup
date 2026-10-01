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

-- saved loadouts survive a round trip; old saves have none
p.loadouts.blade = {"normal", "sword", "normal"}
local with_loadout = Profile.decode(Profile.encode(p))
equal(table.concat(with_loadout.loadouts.blade, ","), "normal,sword,normal")
assert(next(old.loadouts) == nil)

-- Profile.loadout: saved list, filtered to coins you can use and capped
local fresh2 = Profile.new()
equal(table.concat(Profile.loadout(fresh2, "blade", 5), ","), "normal,normal,normal", "default deck")
Profile.set_loadout(fresh2, "blade", {"sword", "hammer", "normal", "normal", "normal", "dagger"})
equal(table.concat(Profile.loadout(fresh2, "blade", 5), ","), "sword,normal,normal,normal,dagger", "hammer is locked, capped at 5")
Profile.set_loadout(fresh2, "blade", {"hammer"})
equal(table.concat(Profile.loadout(fresh2, "blade", 5), ","), "normal,normal,normal", "nothing usable falls back to the deck")

print("profile tests passed")
