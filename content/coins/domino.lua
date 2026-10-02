-- Effect "next_heads": the next coin is guaranteed to land Heads (it can still be changed by relics and the boss).
return {
  name = "Domino", description = "Heads: 2 points, and the next coin lands Heads. Tails: quota +1.",
  rarity = "UR",
  probability = .6,
  heads = {{type = "score", amount = 2}, {type = "next_heads", coins = 1}}, tails = {{type = "penalty", amount = 1}},
}
