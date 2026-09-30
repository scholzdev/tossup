-- Coin registry. Each coin lives in content/coins/<id>.lua and returns its def table.
-- Data fields: name, description, probability, heads, tails (effect lists).
-- Optional hooks (see src/hooks.lua): on_deal, on_odds, on_flip, on_resolve, on_discard,
-- grow, register.
local ORDER = {"normal", "copper", "sword", "lucky", "cursed", "loaded", "dagger", "hammer", "blood", "spark", "focus", "snowball", "gambler", "momentum", "echo", "vampire", "miser", "fuse", "phoenix", "contrarian", "chain", "bank", "lucky_seven", "hourglass", "capacitor", "martyr", "bounty", "jester", "flock"}

local defs = {}
for _, id in ipairs(ORDER) do defs[id] = require("content.coins." .. id) end
return defs
