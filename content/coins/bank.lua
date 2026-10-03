-- Interest on held gold. Hook: on_resolve adds a gold effect.
return {
  name = "Bank", description = "Heads: +3 gold, plus 1 per 10 gold held (max +3).",
  rarity = "R",
  coin_types = {"fortune", "greed"},
  probability = .4,
  heads = {{type = "gold", amount = 3}}, tails = {},
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    local interest = math.min(3, math.floor(game.player.gold / 10))
    if interest > 0 then res.effects[#res.effects + 1] = {type = "gold", amount = interest} end
  end,
}
