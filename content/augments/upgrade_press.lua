local catalog = require("content.coins")

local function choices(game)
  local result = {}
  for _, owned in ipairs(game.coins) do
    if not owned.upgrade then
      local upgrades = catalog[owned.id].upgrades or {}
      local keys = {}
      for key in pairs(upgrades) do keys[#keys + 1] = key end
      table.sort(keys)
      for _, key in ipairs(keys) do
        result[#result + 1] = {key = tostring(owned.uid) .. ":" .. key, coin_id = owned.id,
          uid = owned.uid, upgrade = key, title = catalog[owned.id].name,
          upgrade_name = upgrades[key].name, detail = upgrades[key].description}
      end
    end
  end
  return result
end

return {
  id = "upgrade_press",
  name = "Upgrade Press",
  description = "Choose an owned coin and give it one of its available upgrades.",
  tier = "silver",
  available = function(ctx) return #choices(ctx.game) > 0 end,
  on_pick = function() return {} end,
  valid_pending = function(_, pending) return pending.id == "upgrade_press" end,
  choices = function(ctx) return choices(ctx.game) end,
  on_choose = function(ctx, _, choice)
    for _, owned in ipairs(ctx.game.coins) do
      if owned.uid == choice.uid then
        owned.upgrade = choice.upgrade
        ctx.game.log[#ctx.game.log + 1] = "Upgrade Press: " .. catalog[owned.id].name .. " gained "
          .. catalog[owned.id].upgrades[choice.upgrade].name .. "."
        return
      end
    end
  end,
}
