-- Turns the energy stat into points (makes energy worth saving instead of discarding).
return {
  name = "Capacitor", description = "Heads: 2 points per energy you hold. Tails: +1 energy.",
  rarity = "R",
  probability = .5,
  heads = {}, tails = {{type = "energy", amount = 1}},
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    res.effects[#res.effects + 1] = {type = "score", amount = 2 * game.player.energy}
  end,
}
