-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Dagger", description = "Scores either way.",
  rarity = "N",
  coin_types = {"steel"},
  cost = 12,
  probability = .67,
  heads = {{type = "score", amount = 2}},
  tails = {{type = "score", amount = 1}},
  upgrades = {
    deep_cut = {name = "Deep Cut", cost = 8, description = "Heads scores +1 point.", heads_score = 1},
    steady_hand = {name = "Steady Hand", cost = 9, description = "+6% Heads chance.", heads_probability = .06},
  },
}
