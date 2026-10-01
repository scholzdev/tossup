-- Effect "combo_bonus": the combo (same result in a row) counts extra steps.
return {
  name = "Hot Hand", description = "Heads: 2 points, and the combo grows by 1 extra step.",
  rarity = "R",
  probability = .5,
  heads = {{type = "score", amount = 2}, {type = "combo_bonus", amount = 1}}, tails = {},
}
