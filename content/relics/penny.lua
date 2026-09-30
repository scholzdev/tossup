return {
  name = "Lucky Penny", description = "First Tails each level becomes Heads",
  register = function(ctx)
    local used = false
    ctx.on("encounter_start", function() used = false end)
    ctx.on("coin_outcome", function(e)
      if e.result == "Tails" and not used then
        used = true
        e.result = "Heads"
      end
    end)
  end,
}
