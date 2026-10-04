local catalog = require("content.coins")
local order = require("content.coin_order")
local RNG = require("src.rng")

local epic_ids = {}
for _, id in ipairs(order) do
  if catalog[id].rarity == "UR" then epic_ids[#epic_ids + 1] = id end
end

return {
  id = "epic_windfall",
  name = "Epic Windfall",
  description = "Gain a random Epic coin. If your bank is full, choose a coin to replace.",
  tier = "gold",
  available = function(ctx) return ctx.game.encounter_index + 1 == 6 and #epic_ids > 0 end,
  on_pick = function(ctx)
    local game = ctx.game
    local id = epic_ids[RNG.int(game, 1, #epic_ids)]
    if #game.coins < game.slots then
      ctx.add_coin(id)
      game.log[#game.log + 1] = "Epic Windfall: gained " .. catalog[id].name .. "."
      return
    end
    return {reward_id = id}
  end,
  valid_pending = function(ctx, pending)
    return pending.id == "epic_windfall" and type(pending.reward_id) == "string"
      and catalog[pending.reward_id] ~= nil and catalog[pending.reward_id].rarity == "UR"
      and #ctx.game.coins >= ctx.game.slots
  end,
  choices = function(ctx)
    local choices = {}
    for _, owned in ipairs(ctx.game.coins) do
      choices[#choices + 1] = {key = owned.uid, coin_id = owned.id, current_upgrade = owned.upgrade, title = catalog[owned.id].name,
        detail = "Replace this coin."}
    end
    return choices
  end,
  on_choose = function(ctx, pending, choice)
    ctx.replace_coin(choice.key, pending.reward_id)
    ctx.game.log[#ctx.game.log + 1] = "Epic Windfall: replaced " .. catalog[choice.coin_id].name
      .. " with " .. catalog[pending.reward_id].name .. "."
  end,
}
