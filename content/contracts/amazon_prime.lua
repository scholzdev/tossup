local RNG = require("src.rng")

return {
  id = "amazon_prime",
  name = "AMAZON PRIME",
  description = "Clear with at least 3 unplayed coins remaining.",
  drawback = "Miss it and lose up to 2 coins from your stack.",
  reward = 0,
  reward_text = "HALF PRICE REROLLS",
  complete = function(encounter)
    return #encounter.queue + #encounter.pile >= 3
  end,
  on_success = function(ctx, encounter)
    local game = ctx.game
    if game.phase ~= "SHOP" then return end
    game.reroll_cost = math.max(1, math.floor((game.reroll_cost or 4) / 2))
    game.reroll_step = math.max(1, math.floor((game.reroll_step or 2) / 2))
    game.log[#game.log + 1] = "Amazon Prime: shop rerolls are 50% off."
  end,
  on_failure = function(ctx, encounter)
    local game = ctx.game
    local lost = math.min(2, math.max(0, #game.coins - 1))

    for _ = 1, lost do
      local removed = table.remove(game.coins, RNG.int(game, 1, #game.coins))
      if game.selected_uid == removed.uid then
        game.selected_uid = game.coins[1].uid
      end
    end

    game.log[#game.log + 1] = "Contract penalty: lost " .. lost .. " coins."
  end,
}
