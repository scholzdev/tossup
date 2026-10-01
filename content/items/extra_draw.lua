return {
  name = "Extra Draw", short = "+DRAW", cost = 10,
  description = "A played coin returns to the pile",
  use = function(game)
    local Game = require("src.game")
    local e = game.encounter
    if e.returned >= Game.RETURN_CAP then return false end
    local any = false
    for _, uid in ipairs(e.played) do if not e.discarded[uid] then any = true end end
    if not any then return false end
    Game.apply_effect(game, nil, {type = "extra_draw", amount = 1})
  end,
}
