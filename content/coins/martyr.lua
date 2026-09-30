-- Tails raises the quota; every penalty taken this level is paid back as bonus points on Heads.
-- State lives on the instance and is reset by grow("level").
return {
  name = "Martyr", description = "Tails: quota +3. Heads: 4 points, +1 per Tails so far this level.",
  probability = .5,
  heads = {{type = "score", amount = 4}}, tails = {{type = "penalty", amount = 3}},
  grow = function(inst, event)
    if event == "level" then inst.debt = 0 end
  end,
  on_resolve = function(_, inst, res)
    if res.result == "Tails" then
      inst.debt = (inst.debt or 0) + 1
    elseif (inst.debt or 0) > 0 then
      res.effects[#res.effects + 1] = {type = "score", amount = inst.debt}
    end
  end,
}
