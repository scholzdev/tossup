-- Reads the combo length (already updated for this flip when on_resolve runs); the combo multiplier applies on top.
return {
  name = "Bettor", description = "Heads: 3 points per flip in the current combo (max 30). Tails: quota +2.",
  rarity = "SR",
  energy_cost = 1,
  probability = .35,
  heads = {}, tails = {{type = "penalty", amount = 2}},
  quota_extra = function(_, _, heads) return heads * 6 end,
  on_resolve = function(game, _, res)
    if res.result ~= "Heads" then return end
    res.effects[#res.effects + 1] = {type = "score", amount = math.min(30, 3 * game.encounter.combo_len)}
  end,
}
