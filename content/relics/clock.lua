return {
  name = "Broken Clock", description = "Every 10th flip is Heads",
  register = function(ctx)
    ctx.on("coin_outcome", function(e)
      if e.flips % 10 == 0 then e.result, e.final = "Heads", true end -- final: the House inversion leaves it alone
    end)
  end,
}
