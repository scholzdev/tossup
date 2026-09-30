return {
  name = "Extra Draw", short = "+DRAW", cost = 10,
  description = "+1 draw (max 3 per level)",
  use = function(game)
    local Game = require("src.game")
    if game.encounter.bonus_draws >= 3 then return false end
    Game.apply_effect(game, nil, {type = "extra_draw", amount = 1})
  end,
}
