-- Ignores its sides: a seeded random effect replaces whatever Heads/Tails would give.
local RNG = require("src.rng")

local RESULTS = {
  {type = "score", amount = 6}, {type = "gold", amount = 4},
  {type = "energy", amount = 2}, {type = "score", amount = 2},
}

return {
  name = "Jester", description = "Heads or Tails, it does something random.",
  rarity = "UR",
  coin_types = {"chaos"},
  energy_cost = 1,
  probability = .5,
  heads = {}, tails = {},
  quota_extra = function() return 2 end,
  on_resolve = function(game, _, res)
    local pick = RESULTS[RNG.int(game, 1, #RESULTS)]
    res.effects = {{type = pick.type, amount = pick.amount}}
  end,
}
