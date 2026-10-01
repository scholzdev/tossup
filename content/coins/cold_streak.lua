-- The Tails twin of Chain: Tails pays per Tails in a row (the combo counts both sides).
return {
  name = "Cold Streak", description = "Tails: 2 points per Tails in a row (max 20). Heads: 1 point.",
  rarity = "R",
  probability = .5,
  heads = {{type = "score", amount = 1}}, tails = {},
  on_resolve = function(game, _, res)
    if res.result ~= "Tails" then return end
    res.effects[#res.effects + 1] = {type = "score", amount = math.min(20, 2 * game.encounter.combo_len)}
  end,
}
