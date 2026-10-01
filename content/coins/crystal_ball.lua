-- Effect "bank_discard": the player may discard any one of the next three coins of the bank (free), see Game.discard_bank.
return {
  name = "Crystal Ball", description = "Heads: 5 points. Tails: discard one of the next three coins.",
  rarity = "SR",
  probability = .5,
  heads = {{type = "score", amount = 5}}, tails = {{type = "bank_discard", amount = 1}},
}
