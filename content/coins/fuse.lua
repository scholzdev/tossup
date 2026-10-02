-- Discarding charges it (stored on the coin instance, so it survives between levels).
return {
  name = "Fuse", description = "Discard it to charge +6. Heads spends all charge as points.",
  rarity = "SR",
  cost = 10,
  probability = .3,
  heads = {{type = "score", amount = 1}}, tails = {},
  on_discard = function(_, inst) inst.charge = (inst.charge or 0) + 6 end,
  on_resolve = function(_, inst, res)
    if res.result ~= "Heads" or (inst.charge or 0) == 0 then return end
    res.effects[#res.effects + 1] = {type = "score", amount = inst.charge}
    inst.charge = 0
  end,
}
