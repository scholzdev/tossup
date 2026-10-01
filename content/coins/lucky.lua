-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Lucky", description = "Heads: goes back into the pile and plays again.",
  rarity = "N",
  probability = .5,
  heads = {{type = "extra_draw", amount = 1}},
  tails = {},
}
