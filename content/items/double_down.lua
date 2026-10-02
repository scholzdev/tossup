local DOUBLED = {score = true, gold = true, energy = true, penalty = true}

return {
  name = "Double Down", short = "DOUBLE", cost = 14,
  description = "Next coin: points, gold, energy and penalties x2",
  use = function(_, Items)
    Items.arm("coin_resolve", function(e)
      for _, effect in ipairs(e.res.effects) do
        if DOUBLED[effect.type] then effect.amount = effect.amount * 2 end -- other effects carry no plain amount
      end
    end)
  end,
}
