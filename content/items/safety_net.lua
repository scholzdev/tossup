return {
  name = "Safety Net", short = "SHIELD", cost = 9,
  description = "The next combo break is prevented",
  use = function(game)
    require("src.game").apply_effect(game, nil, {type = "combo_shield", amount = 1})
  end,
}
