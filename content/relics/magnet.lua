-- Relic hooks: register({game, on}) subscribes to bus events for as long as the relic is owned.
-- Every 3rd Heads in a row adds +5% Heads to every coin for the rest of the level.
return {
  name = "Magnet", description = "Every 3 Heads in a row: +5% Heads this level",
  register = function(ctx)
    ctx.on("coin_resolved", function(e)
      local level = e.game.encounter
      if level.streak > 0 and level.streak % 3 == 0 then level.magnet = level.magnet + .05 end
    end)
  end,
}
