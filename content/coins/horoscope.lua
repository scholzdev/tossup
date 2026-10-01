-- Effect "all_odds": every coin gets more Heads chance for the rest of the level (the same pool Focus and Magnet feed).
return {
  name = "Horoscope", description = "Heads: 1 point, all coins +7% Heads this level. Tails: all coins +3%.",
  rarity = "R",
  probability = .5,
  heads = {{type = "score", amount = 1}, {type = "all_odds", amount = .07}}, tails = {{type = "all_odds", amount = .03}},
}
