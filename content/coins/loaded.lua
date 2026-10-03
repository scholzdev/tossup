-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Loaded", description = "Heads: 4 gold. Tails: lose up to 8 gold. Edge: lose up to 4, then gain 2 gold.",
  rarity = "N",
  coin_types = {"fortune", "greed"},
  cost = 12,
  probability = .59,
  tie_probability = .23,
  heads = {{type = "gold", amount = 4}},
  tails = {{type = "gold_loss", amount = 8}},
}
