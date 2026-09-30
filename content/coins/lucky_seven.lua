-- Seeded 1-in-7 jackpot across two hooks (on_flip decides, on_resolve pays). State on the instance.
local RNG = require("src.rng")

return {
  name = "Lucky Seven", description = "1 in 7: lands Heads and pays triple points.",
  probability = .4,
  heads = {{type = "score", amount = 3}}, tails = {},
  on_flip = function(game, inst, flip)
    inst.jackpot = RNG.int(game, 1, 7) == 7
    if inst.jackpot then flip.result = "Heads" end
  end,
  on_resolve = function(_, inst, res)
    if not inst.jackpot then return end
    inst.jackpot = false
    for _, effect in ipairs(res.effects) do effect.amount = effect.amount * 3 end
  end,
}
