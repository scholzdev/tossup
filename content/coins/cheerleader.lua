-- Buffs the odds of the coins that follow: effect "next_odds".
return {
  name = "Cheerleader", description = "Heads: 2 points, next 2 coins +20% Heads. Tails: next coin +20%.",
  rarity = "R",
  probability = .7,
  heads = {{type = "score", amount = 2}, {type = "next_odds", amount = .2, coins = 2}},
  tails = {{type = "next_odds", amount = .2, coins = 1}},
}
