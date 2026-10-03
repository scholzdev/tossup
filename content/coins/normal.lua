-- The baseline coin: nothing special. Cheap, so the shop can top the bank up to five.
return {
  name = "Normal", description = "A plain coin. Barely a scratch.",
  rarity = "N",
  probability = .65, cost = 5,
  heads = {{type = "score", amount = 1}},
  tails = {},
  upgrades = {
    lucky_day = {
      name = "Lucky Day", cost = 8,
      description = "Heads scores +1 point.",
      heads_score = 1,
    },
    mathematician = {
      name = "Mathematician", cost = 10,
      description = "+10% Heads chance.",
      heads_probability = .10,
    },
  },
}
