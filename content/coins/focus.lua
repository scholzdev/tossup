-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Focus", description = "Builds its own Heads chance.",
  rarity = "R",
  cost = 10,
  probability = .5,
  heads = {{type = "probability", amount = 0.15}},
  tails = {{type = "score", amount = 4}},
}
