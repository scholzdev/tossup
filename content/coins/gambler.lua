-- Hooks used: on_resolve with seeded RNG to rewrite or cancel the effects.
local RNG = require("src.rng")

return {
  name = "Gambler", description = "Heads is a bet: 50% triple points, otherwise nothing.",
  probability = .5,
  heads = {{type = "score", amount = 7}},
  tails = {},
  on_resolve = function(game, _, res)
    local Game = require("src.game") -- lazy: src.game loads this file
    if res.result ~= "Heads" then return end
    if RNG.random(game) < .5 then
      for _, effect in ipairs(res.effects) do effect.amount = effect.amount * 3 end
      Game.log(game, "Gambler wins the bet.")
    else
      res.effects = {}
      Game.log(game, "Gambler loses the bet.")
    end
  end,
}
