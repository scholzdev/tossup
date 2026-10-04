-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Focus", description = "Heads: the next coin gets +35% Heads. Tails: 4 points.",
  rarity = "R",
  coin_types = {"fortune"},
  cost = 10,
  probability = .65,
  heads = {{type = "next_odds", amount = .35, coins = 1}},
  tails = {{type = "score", amount = 4}},
  upgrades = {
    clear_mind = {name = "Clear Mind", cost = 10, description = "+8% Heads chance.", heads_probability = .08},
    follow_through = {name = "Follow Through", cost = 10, description = "Heads scores +2 points.", heads_score = 2},
  },
}
