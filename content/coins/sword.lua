-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Sword", description = "Steady points on Heads.",
  rarity = "N",
  coin_types = {"steel"},
  cost = 12,
  probability = .35,
  heads = {{type = "score", amount = 3}},
  tails = {},
}
