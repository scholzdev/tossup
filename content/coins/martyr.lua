-- Tails raises the quota; every Tails flipped this level (by any coin) is paid back as bonus points on Heads.
-- The count lives on the level (game.encounter.tails).
return {
  name = "Martyr", description = "Tails: quota +3. Heads: 4 points, +1 per Tails so far this level.",
  rarity = "SR",
  energy_cost = 1,
  probability = .4,
  heads = {{type = "score", amount = 4}}, tails = {{type = "penalty", amount = 3}},
  on_resolve = function(game, _, res)
    if res.result == "Heads" and (game.encounter.tails or 0) > 0 then
      res.effects[#res.effects + 1] = {type = "score", amount = game.encounter.tails}
    end
  end,
}
