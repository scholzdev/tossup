return {
  id = "scrap_dealer",
  name = "Scrap Dealer",
  description = "Discarding a coin grants 1 gold, but each discard adds 2 quota to the current level.",
  tier = "silver",
  on_trigger = function(ctx)
    if ctx.event ~= "discard" or not ctx.game.run.discard then return end
    local game, encounter = ctx.game, ctx.encounter
    game.player.gold = game.player.gold + 1
    encounter.max_quota = encounter.max_quota + 2
    if not encounter.cleared then encounter.quota = encounter.quota + 2 end
    game.log[#game.log + 1] = "Scrap Dealer: +1 gold, quota +2."
  end,
}
