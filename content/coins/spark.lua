-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Spark", description = "Energy or a small strike.",
  rarity = "R",
  probability = .7,
  heads = {{type = "energy", amount = 2}},
  tails = {{type = "score", amount = 3}},
  upgrades = {
    hot_spark = {name = "Hot Spark", cost = 9, description = "Heads scores +2 points.", heads_score = 2},
    reliable_spark = {name = "Reliable Spark", cost = 10, description = "+7% Heads chance.", heads_probability = .07},
  },
}
