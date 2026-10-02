-- The baseline coin: nothing special. Cheap, so the shop can top the bank up to five.
return {
  name = "Normal", description = "A plain coin. Barely a scratch.",
  rarity = "N",
  probability = .65, cost = 5,
  heads = {{type = "score", amount = 1}},
  tails = {},
}
