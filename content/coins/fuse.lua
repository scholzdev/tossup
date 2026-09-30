-- Discarding charges it (stored on the coin instance, so it survives between levels).
return {
  name = "Fuse", description = "Discard it to charge +4. Heads spends all charge as points.",
  probability = .5,
  heads = {{type = "score", amount = 1}}, tails = {},
  on_discard = function(_, inst) inst.charge = (inst.charge or 0) + 4 end,
  on_resolve = function(_, inst, res)
    if res.result ~= "Heads" or (inst.charge or 0) == 0 then return end
    res.effects[#res.effects + 1] = {type = "score", amount = inst.charge}
    inst.charge = 0
  end,
}
