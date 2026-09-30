-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Dagger", description = "Scores either way.",
  probability = .75,
  heads = {{type = "score", amount = 4}},
  tails = {{type = "score", amount = 1}},
}
