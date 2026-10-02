-- Replays the previous coin's effects for this side. Hook: on_resolve reading game.last_result.
return {
  name = "Echo", description = "Repeats the effects the previous coin had for this side.",
  rarity = "UR",
  probability = .6,
  heads = {}, tails = {},
  on_resolve = function(game, _, res)
    local previous = game.last_result
    if not previous then return end
    local Game = require("src.game") -- lazy: src.game loads this file
    local def = Game.catalog()[Game.get_coin(game, previous.uid).id]
    for _, effect in ipairs(def[string.lower(res.result)]) do
      res.effects[#res.effects + 1] = {type = effect.type, amount = effect.amount, coins = effect.coins}
    end
  end,
}
