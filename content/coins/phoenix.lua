-- Anger builds on Tails and is spent on the next Heads. State lives on the instance.
return {
  name = "Phoenix", description = "Each Tails stores anger (max 5). Heads: 3 points +2 per anger.",
  rarity = "UR",
  probability = .5,
  heads = {{type = "score", amount = 3}}, tails = {{type = "penalty", amount = 2}},
  on_resolve = function(_, inst, res)
    if res.result == "Tails" then
      inst.anger = math.min(5, (inst.anger or 0) + 1)
    else
      for _, effect in ipairs(res.effects) do
        if effect.type == "score" then effect.amount = effect.amount + 2 * (inst.anger or 0) end
      end
      inst.anger = 0
    end
  end,
}
