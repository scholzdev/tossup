-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Dagger", description = "Scores either way.",
  rarity = "N",
  cost = 12,
  probability = .8,
  heads = {{type = "score", amount = 2}},
  tails = {{type = "score", amount = 1}},
}
