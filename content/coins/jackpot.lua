-- A long shot: 20% Heads for a huge payout. Costs energy to flip.
return {
  name = "Jackpot", description = "Heads: 25 points. Only 20% Heads.",
  rarity = "SR", cost = 18,
  energy_cost = 1,
  probability = .2,
  heads = {{type = "score", amount = 25}}, tails = {},
}
