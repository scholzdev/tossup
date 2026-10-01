-- Effect "extra_exchange": one more exchange is allowed this level.
return {
  name = "Lifeline", description = "Heads: 1 point, and you may exchange one more time this level.",
  rarity = "R",
  probability = .5,
  heads = {{type = "score", amount = 1}, {type = "extra_exchange", amount = 1}}, tails = {},
}
