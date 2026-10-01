return {
  name = "Shortcut", short = "+3 PTS", cost = 12,
  description = "Score 3 points at once",
  use = function(game)
    require("src.game").apply_effect(game, nil, {type = "score", amount = 3}) -- lazy: src.game loads this file
  end,
}
