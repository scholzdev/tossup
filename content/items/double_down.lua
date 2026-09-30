return {
  name = "Double Down", short = "DOUBLE", cost = 14,
  description = "Next coin effects x2",
  use = function(_, Items)
    Items.arm("coin_resolve", function(e)
      for _, effect in ipairs(e.res.effects) do effect.amount = effect.amount * 2 end
    end)
  end,
}
