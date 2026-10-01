-- Effect "peek": shows the next two coins of the draw pile (like the Peek chip).
return {
  name = "Crystal Ball", description = "Heads: 3 points and a look at the next two coins of the pile. Tails: 1 point.",
  rarity = "SR",
  probability = .5,
  heads = {{type = "score", amount = 3}, {type = "peek", amount = 1}}, tails = {{type = "score", amount = 1}},
}
