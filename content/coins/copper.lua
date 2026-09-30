-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Copper", description = "Funds your next move.",
  probability = .5,
  heads = {{type = "gold", amount = 2}},
  tails = {{type = "energy", amount = 1}},
}
