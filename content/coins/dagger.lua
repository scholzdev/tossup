-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Dagger", description = "Scores either way.",
  rarity = "N",
  coin_types = {"steel"},
  cost = 12,
  probability = .67,
  heads = {{type = "score", amount = 2}},
  tails = {{type = "score", amount = 1}},
}
