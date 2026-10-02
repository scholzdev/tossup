-- Heads: copies the Heads effects of a random other coin in your deck (seeded), as printed on that coin.
local RNG = require("src.rng")

return {
  name = "Mimic", description = "Heads: does what the Heads side of a random other coin in your deck does.",
  rarity = "UR",
  probability = .3,
  heads = {}, tails = {{type = "score", amount = 1}},
  on_resolve = function(game, inst, res)
    if res.result ~= "Heads" then return end
    local Game = require("src.game") -- lazy: src.game loads this file
    local others = {}
    for _, owned in ipairs(game.coins) do
      if owned ~= inst and #Game.catalog()[owned.id].heads > 0 then others[#others + 1] = owned end
    end
    if #others == 0 then return end
    local pick = others[RNG.int(game, 1, #others)]
    for _, effect in ipairs(Game.catalog()[pick.id].heads) do
      res.effects[#res.effects + 1] = {type = effect.type, amount = effect.amount, coins = effect.coins}
    end
    Game.log(game, "Mimic copies " .. Game.catalog()[pick.id].name .. ".")
  end,
}
