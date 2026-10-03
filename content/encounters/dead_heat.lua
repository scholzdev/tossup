return {
  id = "dead_heat",
  name = "Dead Heat",
  description = "A Tie breaks your combo and burns its pot. Banking a combo grants 2 extra gold.",
  on_trigger = function(ctx)
    local game = ctx.game
    if ctx.event == "combo" then
      local throw = game.run.throw
      if throw and throw.result == "Tie" and ctx.encounter.combo_side then
        ctx.encounter.combo_side, ctx.encounter.combo_len = nil, 0
      end
    elseif ctx.event == "bank" and game.run.bank then
      game.run.bank.amount = game.run.bank.amount + 2
    end
  end,
}
