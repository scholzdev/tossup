return {
  id = "bankers_cut",
  name = "Banker's Cut",
  description = "Banked combo pots grant 2 extra gold. Each bank adds 2 quota to the next level.",
  tier = "silver",
  on_trigger = function(ctx)
    if ctx.event ~= "bank" then return end
    local bank = ctx.game.run.bank
    if not bank then return end
    bank.amount = bank.amount + 2
    ctx.game.next_level_quota_bonus = (ctx.game.next_level_quota_bonus or 0) + 2
  end,
}
