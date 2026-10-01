-- Always lands like the previous flip (the mirror image of Contrarian). Hook: on_flip rewrites the outcome.
return {
  name = "Twin", description = "Always lands the same as the previous flip.",
  rarity = "SR",
  probability = .5,
  heads = {{type = "score", amount = 4}}, tails = {{type = "score", amount = 1}},
  on_flip = function(game, _, flip)
    local previous = game.last_result
    if previous then flip.result = previous.final end
  end,
}
