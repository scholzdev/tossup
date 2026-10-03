-- Reads the gold stat. Hook: on_resolve appends a computed effect.
return {
  name = "Miser", description = "Heads: 1 point per 10 gold you hold.",
  rarity = "R",
  coin_types = {"greed"},
  probability = .4,
  heads = {}, tails = {{type = "gold", amount = 2}},
  quota_extra = function(game, _, heads) return heads * math.floor(game.player.gold / 10) end,
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    local amount = math.floor(game.player.gold / 10)
    if amount > 0 then res.effects[#res.effects + 1] = {type = "score", amount = amount} end
  end,
}
