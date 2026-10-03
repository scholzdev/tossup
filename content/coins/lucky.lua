-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Lucky", description = "Heads: 2 points, and it goes back into the pile to play again.",
  rarity = "N",
  coin_types = {"fortune"},
  cost = 10,
  probability = .3,
  heads = {{type = "score", amount = 2}, {type = "extra_draw", amount = 1}},
  tails = {},
}
