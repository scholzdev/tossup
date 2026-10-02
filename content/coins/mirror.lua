-- Effect "next_swap": the next coin resolves with the effects of its other side.
return {
  name = "Mirror", description = "Heads: the next coin uses the effects of its other side. Tails: 2 points.",
  rarity = "SR",
  probability = .65,
  heads = {{type = "score", amount = 1}, {type = "next_swap", coins = 1}}, tails = {{type = "score", amount = 2}},
}
