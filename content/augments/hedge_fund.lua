return {
  id = "hedge_fund",
  name = "Hedge Fund",
  description = "A winning side bet pays 25% extra. Losing one adds 2 quota to the next level.",
  tier = "silver",
  on_trigger = function(ctx)
    local bet = ctx.game.run.side_bet
    if not bet then return end
    if ctx.event == "side_bet_quote" then
      bet.payout = math.floor(bet.payout * 1.25 + .5)
    elseif ctx.event == "side_bet_settled" and bet.outcome == "LOST" then
      ctx.game.next_level_quota_bonus = (ctx.game.next_level_quota_bonus or 0) + 2
    end
  end,
}
