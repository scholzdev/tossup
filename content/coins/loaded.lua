-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Loaded", description = "Reliable income on Heads.",
  probability = .75,
  heads = {{type = "gold", amount = 4}},
  tails = {},
}
