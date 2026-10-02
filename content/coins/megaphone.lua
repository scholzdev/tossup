-- Buffs the coins that follow: effect "next_mult" (amount = factor, coins = how many coins it lasts).
return {
  name = "Megaphone", description = "Heads: 2 points, and the next 2 coins pay double.",
  rarity = "R", cost = 20,
  energy_cost = 1,
  probability = .65,
  heads = {{type = "score", amount = 2}, {type = "next_mult", amount = 2, coins = 2}}, tails = {},
}
