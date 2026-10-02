-- Reads the Heads streak tracked by the game (already includes this flip when on_resolve runs).
return {
  name = "Chain", description = "Heads: 2 points per Heads in a row, including this one.",
  rarity = "R",
  probability = .6,
  heads = {}, tails = {},
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    res.effects[#res.effects + 1] = {type = "score", amount = 2 * game.encounter.streak}
  end,
}
