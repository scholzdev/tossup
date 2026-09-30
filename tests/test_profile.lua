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

print("profile tests passed")
