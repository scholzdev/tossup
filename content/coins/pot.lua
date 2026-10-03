-- The pot grows with every flip of the level. e.flips already counts the flip being resolved.
return {
  name = "Pot", description = "Heads: points equal to the flips made so far this level (max 8).",
  rarity = "R",
  probability = .25,
  heads = {}, tails = {{type = "score", amount = 1}},
  quota_extra = function(game, _, heads) return heads * math.min(8, (#game.coins + 1) / 2) end,
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    res.effects[#res.effects + 1] = {type = "score", amount = math.min(8, game.encounter.flips)}
  end,
}
