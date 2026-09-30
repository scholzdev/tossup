return {
  name = "Broken Clock", description = "Every 10th flip is Heads",
  register = function(ctx)
    ctx.on("coin_outcome", function(e)
      if e.flips % 10 == 0 then e.result = "Heads" end
    end)
  end,
}
