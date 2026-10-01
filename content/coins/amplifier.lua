-- Effect "amplify": all active "next coins" buffs last one coin longer and get stronger.
return {
  name = "Amplifier", description = "Heads: 1 point, and all active buffs last 1 coin longer and get stronger. Tails: 1 point.",
  rarity = "SR",
  energy_cost = 1,
  probability = .5,
  heads = {{type = "score", amount = 1}, {type = "amplify", amount = 1}}, tails = {{type = "score", amount = 1}},
}
