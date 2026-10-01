-- on_odds is pure: it is evaluated every time the odds are displayed.
return {
  name = "Hourglass", description = "+30% Heads when 3 or fewer draws remain.",
  rarity = "R",
  probability = .4,
  heads = {{type = "score", amount = 4}}, tails = {{type = "extra_draw", amount = 1}},
  on_odds = function(game, _, odds)
    local level = game.encounter
    if level and level.draws <= 3 then odds.p = odds.p + .3 end
  end,
}
