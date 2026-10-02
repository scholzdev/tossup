local Game = require("src.game")
local Profile = require("src.profile")

local function accepts(character, unlocked, deck)
  return pcall(Game.new, 7, character, unlocked, deck)
end

assert(Profile.rarity_limit("normal") == 3)
assert(Profile.rarity_limit("hammer") == 2)
assert(Profile.rarity_limit("cursed") == 1)
assert(Profile.rarity_limit("mimic") == 1)
assert(accepts("blade", {"hammer"}, {"normal", "normal", "normal", "hammer", "hammer"}))
assert(not accepts("blade", nil, {"normal", "normal", "normal", "normal"}))
assert(not accepts("blade", nil, {"normal", "sword", "dagger", "normal"}))
assert(accepts("blade", {"hammer"}, {"hammer", "hammer"}))
assert(not accepts("blade", {"hammer"}, {"hammer", "hammer", "hammer"}))
assert(not accepts("blade", {"hammer", "blood"}, {"hammer", "blood", "hammer"}))
assert(not accepts("blade", {"cursed"}, {"cursed", "cursed"}))
assert(not accepts("blade", {"cursed", "martyr"}, {"cursed", "martyr"}))
assert(not accepts("seer", {"mimic"}, {"mimic", "mimic"}))
assert(not accepts("seer", {"mimic", "echo"}, {"mimic", "echo"}))

print("copy limit tests passed")
