return {
  id = "all_in",
  name = "All-In",
  description = "The first successful push each level doubles its combo payout. A failed push costs 2 gold.",
  tier = "silver",
  on_trigger = function(ctx)
    if ctx.event ~= "push" then return end
    local game, encounter = ctx.game, ctx.encounter
    local push = game.run.push
    if not push then return end
    if push.result ~= "Tie" and push.result == push.combo_side and not encounter.all_in_paid then
      encounter.all_in_paid = true
      push.multiplier = 2
      push.message = "All-In push succeeded: combo payout doubled."
    else
      local lost = math.min(2, game.player.gold)
      game.player.gold = game.player.gold - lost
      push.message = "All-In push failed: -" .. lost .. " gold."
    end
  end,
}
