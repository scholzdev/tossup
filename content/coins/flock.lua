-- Synergy with duplicates in the bank. on_odds is pure.
return {
  name = "Flock", description = "+10% Heads for every other Flock in your bank.",
  rarity = "R",
  probability = .4,
  heads = {{type = "score", amount = 4}}, tails = {},
  on_odds = function(game, inst, odds)
    for _, other in ipairs(game.coins) do
      if other ~= inst and other.id == inst.id then odds.p = odds.p + .1 end
    end
  end,
}
