return {
  id = "high_roller_table",
  name = "High-Roller Table",
  description = "Side bets use double the stake and payout. A Tie loses the stake.",
  on_trigger = function(ctx)
    local run = ctx.game.run
    if ctx.event == "side_bet_quote" and run.side_bet then
      run.side_bet.stake = run.side_bet.stake * 2
      run.side_bet.payout = run.side_bet.payout * 2
    elseif ctx.event == "side_bet_resolve" and run.throw and run.throw.result == "Tie" then
      run.side_bet.settled = true
      run.side_bet.outcome = "LOST"
    end
  end,
}
