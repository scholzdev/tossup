-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Blood", description = "Heads: 10 points. Tails: quota +6. Edge: half of both.",
  rarity = "R",
  coin_types = {"blood"},
  cost = 22,
  energy_cost = 1,
  probability = .37,
  tie_probability = .09,
  heads = {{type = "score", amount = 10}},
  tails = {{type = "penalty", amount = 6}},
}
