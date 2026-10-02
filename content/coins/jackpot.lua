-- A long shot: 15% Heads for a huge payout. Costs energy to flip.
return {
  name = "Jackpot", description = "Heads: 25 points. Only 15% Heads.",
  rarity = "SR", cost = 18,
  energy_cost = 1,
  probability = .15,
  heads = {{type = "score", amount = 25}}, tails = {},
}
