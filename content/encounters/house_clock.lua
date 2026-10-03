return {
  id = "house_clock",
  name = "The House's Clock",
  description = "Every fifth flip is inverted. Clearing a level pays 25% more.",
  on_trigger = function(ctx)
    if ctx.event == "throw" then
      local throw = ctx.game.run.throw
      if throw and throw.count % 5 == 0 and throw.result ~= "Tie" and not throw.stage_inverted then
        throw.result = throw.result == "Heads" and "Tails" or "Heads"
        throw.altered = (throw.altered and throw.altered .. " + " or "") .. "THE HOUSE'S CLOCK"
      end
    elseif ctx.event == "encounter_start" and ctx.encounter.payout then
      ctx.encounter.payout = math.floor(ctx.encounter.payout * 1.25 + .5)
    end
  end,
}
