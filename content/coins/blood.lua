-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Blood", description = "Heads: 10 points. Tails: quota +6. Edge: half of both.",
  rarity = "R",
  coin_types = {"blood"},
  cost = 22,
  energy_cost = 1,
  probability = .37,
  tie_probability = .09,
  heads = {{type = "score", amount = 10}},
  tails = {{type = "penalty", amount = 6}},
  upgrades = {
    bloodletting = {name = "Bloodletting", cost = 12, description = "Heads scores +2 points.", heads_score = 2},
    sure_strike = {name = "Sure Strike", cost = 13, description = "+6% Heads chance.", heads_probability = .06},
  },
}
