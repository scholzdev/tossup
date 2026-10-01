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

print("profile tests passed")
