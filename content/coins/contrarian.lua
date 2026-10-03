-- Always lands opposite of the previous flip. Hook: on_flip rewrites the outcome.
return {
  name = "Contrarian", description = "Always lands opposite of the previous flip.",
  rarity = "SR",
  coin_types = {"chaos"},
  probability = .65,
  heads = {{type = "score", amount = 4}}, tails = {{type = "score", amount = 1}},
  on_flip = function(game, _, flip)
    local previous = game.last_result
    if previous then flip.result = previous.final == "Heads" and "Tails" or "Heads" end
  end,
}
