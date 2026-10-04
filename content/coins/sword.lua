-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Sword", description = "Steady points on Heads.",
  rarity = "N",
  coin_types = {"steel"},
  cost = 12,
  probability = .35,
  heads = {{type = "score", amount = 3}},
  tails = {},
  upgrades = {
    keen_edge = {name = "Keen Edge", cost = 8, description = "Heads scores +1 point.", heads_score = 1},
    true_aim = {name = "True Aim", cost = 10, description = "+8% Heads chance.", heads_probability = .08},
  },
}
