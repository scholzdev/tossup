-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Blood", description = "Big points, but Tails raises the quota.",
  rarity = "R",
  probability = .6,
  heads = {{type = "score", amount = 11}},
  tails = {{type = "penalty", amount = 3}},
}
