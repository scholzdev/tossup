-- Turns the energy stat into points (makes energy worth saving instead of discarding).
return {
  name = "Flux Capacitor", description = "Heads: 2 points per energy you hold. Tails: +1 energy.",
  rarity = "R",
  energy_cost = 1,
  probability = .35,
  heads = {}, tails = {{type = "energy", amount = 1}},
  quota_extra = function(game, _, heads) return heads * 2 * math.max(0, game.player.max_energy - 1) end,
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    res.effects[#res.effects + 1] = {type = "score", amount = 2 * game.player.energy}
  end,
}
