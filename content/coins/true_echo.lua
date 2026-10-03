-- Repeats the previous coin's real effects (after its own hooks, before multipliers), whichever side it landed on.
-- Echo copies the printed values; True Echo copies what actually happened: Snowball growth, Bettor's bonus, buffs.
return {
  name = "True Echo", description = "Repeats what the previous coin really did, including its buffs and growth, on either side.",
  rarity = "UR",
  probability = .5,
  heads = {}, tails = {},
  on_resolve = function(game, _, res)
    local previous = game.last_result
    if not previous or not previous.base_effects then return end
    for _, effect in ipairs(previous.base_effects) do
      res.effects[#res.effects + 1] = {type = effect.type, amount = effect.amount, coins = effect.coins,
        kind = effect.kind}
    end
  end,
}
