return {
  name = "Baton", description = "Combo: +0.4 per step instead of 0.25, up to x4",
  register = function(ctx)
    ctx.on("encounter_start", function(e)
      e.encounter.combo_step, e.encounter.combo_cap = 0.4, 4
    end)
  end,
}
