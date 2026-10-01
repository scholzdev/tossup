return {
  name = "Metronome", description = "Every 4th flip pays double",
  register = function(ctx)
    ctx.on("coin_resolve", function(e)
      if e.game.encounter.flips % 4 ~= 0 then return end
      for _, effect in ipairs(e.res.effects) do
        if effect.type == "score" or effect.type == "gold" then effect.amount = effect.amount * 2 end
      end
    end)
  end,
}
