-- Coin def. Data fields (name, probability, heads, tails) plus optional hooks; see src/hooks.lua.
return {
  name = "Copper", description = "Funds your next move.",
  rarity = "N",
  coin_types = {"greed"},
  cost = 10,
  probability = .3,
  heads = {{type = "gold", amount = 2}},
  tails = {{type = "energy", amount = 1}},
  upgrades = {
    copper_lining = {name = "Copper Lining", cost = 8, description = "Heads scores +1 point.", heads_score = 1},
    bright_side = {name = "Bright Side", cost = 9, description = "+8% Heads chance.", heads_probability = .08},
  },
}
