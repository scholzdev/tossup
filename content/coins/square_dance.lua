return {
  name = "Square Dance",
  description = "Heads: 2 points times the square of Square Dance copies in your deck.",
  heads_description = "1/2/3 copies: 2/8/18 points",
  rarity = "N", cost = 14, probability = .27,
  heads = {}, tails = {},
  quota_extra = function(_, _, heads, _, counts)
    return heads * 2 * counts.square_dance ^ 2
  end,
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    local copies = 0
    for _, owned in ipairs(game.coins) do if owned.id == "square_dance" then copies = copies + 1 end end
    res.effects[#res.effects + 1] = {type = "score", amount = 2 * copies * copies}
  end,
}
