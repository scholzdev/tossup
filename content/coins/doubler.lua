-- Every Doubler flip this level doubles the next one's Heads value (the counter is shared by all copies and resets each level).
return {
  name = "Doubler", description = "Heads: 3 points, doubled for every Doubler flip so far this level (3, 6, 12, 24... up to 384).",
  rarity = "SR",
  probability = .3,
  heads = {}, tails = {},
  on_resolve = function(game, _, res)
    local e = game.encounter
    local flips = e.doubler or 0
    e.doubler = flips + 1
    if res.result == "Heads" then
      res.effects[#res.effects + 1] = {type = "score", amount = 3 * 2 ^ math.min(flips, 7)}
    end
  end,
}
