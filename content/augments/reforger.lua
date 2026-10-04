local catalog = require("content.coins")
local order = require("content.coin_order")
local RNG = require("src.rng")

local function alternatives(owned)
  local result = {}
  for _, id in ipairs(order) do
    if catalog[id].rarity == catalog[owned.id].rarity and id ~= owned.id then result[#result + 1] = id end
  end
  return result
end

return {
  id = "reforger",
  name = "Reforger",
  description = "Reforge one coin now into a random different coin of the same rarity. Its upgrade is lost.",
  tier = "silver",
  available = function(ctx)
    for _, owned in ipairs(ctx.game.coins) do if #alternatives(owned) > 0 then return true end end
    return false
  end,
  on_pick = function() return {} end,
  valid_pending = function(_, pending) return pending.id == "reforger" end,
  choices = function(ctx)
    local choices = {}
    for _, owned in ipairs(ctx.game.coins) do
      if #alternatives(owned) > 0 then
        choices[#choices + 1] = {key = owned.uid, coin_id = owned.id, current_upgrade = owned.upgrade, title = catalog[owned.id].name,
          detail = "Random coin of the same rarity."}
      end
    end
    return choices
  end,
  on_choose = function(ctx, _, choice)
    local ids = alternatives({id = choice.coin_id})
    local result = ids[RNG.int(ctx.game, 1, #ids)]
    ctx.replace_coin(choice.key, result)
    ctx.game.log[#ctx.game.log + 1] = "Reforged " .. catalog[choice.coin_id].name
      .. " into " .. catalog[result].name .. "."
  end,
}
