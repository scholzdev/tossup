-- Hooks used: grow("flip") for run-long growth, on_resolve to edit the effect list.
return {
  name = "Snowball", description = "Heads gains +1 point every flip for the whole run (max +8).",
  rarity = "SR",
  cost = 24,
  energy_cost = 1,
  probability = .3,
  heads = {{type = "score", amount = 3}},
  tails = {{type = "score", amount = 1}},
  grow = function(inst, event)
    if event == "flip" then inst.stack = math.min(8, (inst.stack or 0) + 1) end
  end,
  on_resolve = function(_, inst, res)
    for _, effect in ipairs(res.effects) do
      if res.result == "Heads" and effect.type == "score" then effect.amount = effect.amount + (inst.stack or 0) end
    end
  end,
}
