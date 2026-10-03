-- Hooks used: on_odds (pure) to read the level's Heads streak.
return {
  name = "Momentum", description = "+5% Heads for every Heads in a row this level.",
  rarity = "SR",
  coin_types = {"rhythm"},
  probability = .35,
  heads = {{type = "score", amount = 6}},
  tails = {},
  on_odds = function(game, _, odds)
    local e = game.encounter
    if e then odds.p = odds.p + .05 * e.streak end
  end,
}
