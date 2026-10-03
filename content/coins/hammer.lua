-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Hammer", description = "A rare but crushing hit.",
  rarity = "R",
  coin_types = {"steel"},
  cost = 22,
  energy_cost = 2,
  probability = .25,
  heads = {{type = "score", amount = 16}},
  tails = {{type = "score", amount = 1}},
}
