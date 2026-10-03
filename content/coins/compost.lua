return {
  name = "Compost",
  description = "Tails: quota +2; once per level, Fortune coins gain +11% Heads for the run (max +55%).",
  rarity = "N", cost = 10, probability = .47,
  heads = {{type = "score", amount = 2}},
  tails = {{type = "penalty", amount = 2}, {type = "fortune_odds", amount = .11}},
}
