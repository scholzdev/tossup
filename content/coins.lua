-- Coin registry. Each coin lives in content/coins/<id>.lua and returns its def table.
-- Data fields: name, description, rarity (N/R/SR/UR), probability, heads, tails (effect lists),
-- optional energy_cost (energy paid to flip it; default 0).
-- Optional upgrades are keyed by id; each has a name, cost, description, and optional heads_score /
-- heads_probability bonuses. Their costs are added to the coin's shop price.
-- Optional quota_extra(game, inst, heads, tails, counts, distinct): expected net points from scoring hooks
-- not already present in the printed heads/tails effects. Used only to scale the next level's quota.
-- Optional hooks (see src/hooks.lua): on_deal, on_odds, on_flip, on_resolve, on_discard,
-- grow, register.
local ORDER = require("content.coin_order")

local defs = {}
for _, id in ipairs(ORDER) do defs[id] = require("content.coins." .. id) end
return defs
