local RNG = require("src.rng")

return {
  id = "upgrade",
  name = "Upgrade",
  description = "Start with a random Common coin.",
  on_trigger = function(ctx)
    if ctx.event ~= "run_start" then return end
    local pool = ctx.coins_of_rarity("N")
    if #pool == 0 then return end
    local id = pool[RNG.int(ctx.game, 1, #pool)]
    ctx.add_coin(id)
  end,
}
