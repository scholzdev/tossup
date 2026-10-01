-- register(ctx): subscribe to a bus event yourself. Reacts to effects the game actually applied.
return {
  name = "Bounty", description = "Pays 1 gold for every 2 points it scores.",
  rarity = "SR",
  energy_cost = 1,
  probability = .5,
  heads = {{type = "score", amount = 4}}, tails = {{type = "score", amount = 2}},
  register = function(ctx)
    ctx.on("effect_applied", function(e)
      if e.effect.type == "score" then
        e.game.player.gold = e.game.player.gold + math.floor(e.effect.amount / 2)
      end
    end)
  end,
}
