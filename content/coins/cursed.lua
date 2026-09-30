-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Cursed", description = "A powerful, dangerous wager.",
  probability = .25,
  heads = {{type = "score", amount = 15}},
  tails = {{type = "penalty", amount = 2}},
}
