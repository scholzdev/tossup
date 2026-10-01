-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Cursed", description = "A powerful, dangerous wager.",
  rarity = "SR",
  probability = .30,
  heads = {{type = "score", amount = 15}},
  tails = {{type = "penalty", amount = 2}},
}
