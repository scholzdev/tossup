-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Lucky", description = "Makes room for one more flip.",
  rarity = "N",
  probability = .5,
  heads = {{type = "extra_draw", amount = 1}},
  tails = {},
}
