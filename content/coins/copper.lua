-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Copper", description = "Funds your next move.",
  rarity = "N",
  coin_types = {"greed"},
  cost = 10,
  probability = .3,
  heads = {{type = "gold", amount = 2}},
  tails = {{type = "energy", amount = 1}},
}
