-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Hammer", description = "A rare but crushing hit.",
  rarity = "R",
  energy_cost = 2,
  probability = .35,
  heads = {{type = "score", amount = 16}},
  tails = {{type = "score", amount = 1}},
}
