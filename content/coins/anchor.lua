-- Effect "combo_shield": the next result that would break the combo is ignored instead (once per shield).
return {
  name = "Anchor", description = "Heads: 2 points, and the next time the combo would break it holds instead.",
  rarity = "R",
  probability = .5,
  heads = {{type = "score", amount = 2}, {type = "combo_shield", amount = 1}}, tails = {{type = "score", amount = 1}},
}
