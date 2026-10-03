return {
  name = "Good Dog",
  description = "Heads: 2 points; return the highest-scoring coin played this level to the draw pile. Once per level.",
  rarity = "UR", cost = 28, probability = .43,
  heads = {{type = "score", amount = 2}, {type = "fetch_best"}}, tails = {},
}
