local catalog = require("content.coins")

local function owned_types(game)
  local found, types = {}, {}
  for _, owned in ipairs(game.coins) do
    for _, kind in ipairs(catalog[owned.id].coin_types or {}) do
      if not found[kind] then found[kind] = true types[#types + 1] = kind end
    end
  end
  table.sort(types)
  return types
end

return {
  id = "type_specialist",
  name = "Type Specialist",
  description = "Choose a coin type you own. Each coin of that type scores +1 on its first Heads each level.",
  tier = "silver",
  available = function(ctx) return #owned_types(ctx.game) > 0 end,
  on_pick = function() return {} end,
  valid_pending = function(_, pending) return pending.id == "type_specialist" end,
  selected_label = function(game)
    local kind = game.augment_data and game.augment_data.type_specialist
    return kind and kind:upper()
  end,
  choices = function(ctx)
    local choices = {}
    for _, kind in ipairs(owned_types(ctx.game)) do
      choices[#choices + 1] = {key = kind, title = kind:upper(), detail = "First Heads per coin each level: +1 point"}
    end
    return choices
  end,
  on_choose = function(ctx, _, choice)
    ctx.game.augment_data = ctx.game.augment_data or {}
    ctx.game.augment_data.type_specialist = choice.key
    ctx.game.log[#ctx.game.log + 1] = "Type Specialist: " .. choice.key .. "."
  end,
  on_trigger = function(ctx)
    if ctx.event ~= "coin_effects" or not ctx.encounter then return end
    local kind = ctx.game.augment_data and ctx.game.augment_data.type_specialist
    local playing = ctx.game.run.coin_effects
    if not kind or not playing or playing.result ~= "Heads" then return end
    local paid = ctx.encounter.type_specialist_paid or {}
    if paid[playing.coin.uid] then return end
    for _, coin_type in ipairs(catalog[playing.coin.id].coin_types or {}) do
      if coin_type == kind then
        paid[playing.coin.uid] = true
        ctx.encounter.type_specialist_paid = paid
        playing.effects[#playing.effects + 1] = {type = "score", amount = 1}
        return
      end
    end
  end,
}
