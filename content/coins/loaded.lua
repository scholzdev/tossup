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
  upgrades = {
    safer_bet = {name = "Safer Bet", cost = 10, description = "+7% Heads chance.", heads_probability = .07},
    gilded_face = {name = "Gilded Face", cost = 9, description = "Heads scores +2 points.", heads_score = 2},
  },
}
