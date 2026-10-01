-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Sword", description = "Steady points on Heads.",
  rarity = "N",
  cost = 12,
  probability = .5,
  heads = {{type = "score", amount = 5}},
  tails = {},
}
