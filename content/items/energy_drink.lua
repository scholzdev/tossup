return {
  name = "Energy Drink", short = "+2 NRG", cost = 8,
  description = "Gain 2 energy",
  use = function(game) game.player.energy = game.player.energy + 2 end,
}
