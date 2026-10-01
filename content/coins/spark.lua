-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Spark", description = "Energy or a small strike.",
  rarity = "R",
  probability = .5,
  heads = {{type = "energy", amount = 2}},
  tails = {{type = "score", amount = 3}},
}
