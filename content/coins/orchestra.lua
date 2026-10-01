-- The opposite of Flock: variety. Counts the different coin types in the whole deck.
return {
  name = "Orchestra", description = "Heads: 2 points per different coin type in your deck.",
  rarity = "R",
  probability = .5,
  heads = {}, tails = {{type = "gold", amount = 1}},
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    local seen, kinds = {}, 0
    for _, owned in ipairs(game.coins) do
      if not seen[owned.id] then seen[owned.id] = true kinds = kinds + 1 end
    end
    res.effects[#res.effects + 1] = {type = "score", amount = 2 * kinds}
  end,
}
