local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_face, coin_hover, effects, effect_description =
  D.coin_image, D.coin_face, D.coin_hover, D.effects, D.effect_description
local Items = require("src.items")
local catalog, characters = ui.catalog, ui.characters

local function draw_shop()
  box(292, 29, 964, 123, C.panel)
  outline(292, 29, 964, 123, C.gold)
  text("THE SHOP", 310, 38, ui.f48, C.gold)
  text("BUILD YOUR DECK FOR THE NEXT LEVEL", 312, 105, ui.f16, C.face)
  text("GOLD  " .. ui.game.player.gold, 1045, 43, ui.f20, C.gold)
  button("NEXT LEVEL", 1005, 82, 235, 53, C.green, function() Game.leave_shop(ui.game) end)
  local relic = ui.game.shop_relic and Game.relics()[ui.game.shop_relic]
  if relic then
    text("RELIC: " .. relic.name:upper(), 660, 43, ui.f16, C.orange)
    text(relic.description, 660, 62, ui.f16, C.muted)
    button("BUY RELIC / 25", 780, 82, 200, 53, C.orange,
      function() Game.buy_relic(ui.game) end, ui.game.player.gold >= 25)
  end
  if #ui.game.relics > 0 then
    local names = {}
    for i, id in ipairs(ui.game.relics) do names[i] = Game.relics()[id].name end
    text("RELICS: " .. table.concat(names, ", "), 312, 130, ui.f16, C.orange)
  end
  if #ui.game.items > 0 then
    local names = {}
    for i, id in ipairs(ui.game.items) do names[i] = ui.item_catalog[id].short end
    text("ITEMS: " .. table.concat(names, ", "), 660, 130, ui.f16, C.orange)
  end

  text("COIN OFFERS", 300, 165, ui.f20, C.gold)
  button("REROLL / 4", 1053, 159, 187, 39, C.orange,
    function() Game.reroll_shop(ui.game) end, ui.game.player.gold >= 4)
  for i = 1, 4 do
    local x = 300 + (i - 1) * 235
    local id = ui.game.shop_offers[i]
    box(x, 207, 220, 273, C.panel)
    outline(x, 207, 220, 273, id and C.panel_light or C.slot)
    if id then
      local offer = catalog[id]
      coin_image(id, x + 54, 212, 112)
      centered(offer.name:upper(), x, 322, 220, ui.f32, C.face)
      centered(math.floor(offer.probability * 100 + .5) .. "% HEADS", x, 354, 220, ui.f16, C.gold)
      text("H  " .. effects(offer.heads), x + 13, 379, ui.f16, C.blue)
      text("T  " .. effects(offer.tails), x + 13, 402, ui.f16, C.red)
      coin_hover(id, x, 207, 220, 220)
      local cost = offer.cost or 15
      local full = #ui.game.coins >= Game.DECK_MAX
      button(full and "DECK FULL" or ("BUY / " .. cost), x + 11, 435, 198, 37, C.blue,
        function() Game.buy(ui.game, i) end, ui.game.player.gold >= cost and not full)
    else
      centered("SOLD", x, 320, 220, ui.f32, C.muted)
    end
  end

  text("UPGRADES & SERVICES", 300, 496, ui.f20, C.gold)
  local selected = Game.get_coin(ui.game, ui.game.selected_uid)
  for i = 1, 4 do
    local x = 300 + (i - 1) * 235
    box(x, 525, 220, 122, C.panel)
    outline(x, 525, 220, 122, C.panel_light)
  end
  centered("ODDS TUNER", 300, 532, 220, ui.f20, C.gold)
  centered("SELECTED COIN +10% H", 300, 559, 220, ui.f16, C.muted)
  button("UPGRADE / 10", 311, 598, 198, 40, C.gold,
    function() Game.upgrade(ui.game, ui.game.selected_uid) end,
    ui.game.player.gold >= 10 and selected and Game.probability(ui.game, selected) < 1)
  for slot = 1, 2 do
    local x = 535 + (slot - 1) * 235
    local id = ui.game.shop_items[slot]
    if id then
      local def = ui.item_catalog[id]
      centered(def.name:upper(), x, 532, 220, ui.f20, C.orange)
      centered(def.description, x, 559, 220, ui.f16, C.muted)
      button("BUY / " .. def.cost, x + 11, 598, 198, 40, C.orange,
        function() Game.buy_item(ui.game, slot) end,
        ui.game.player.gold >= def.cost and #ui.game.items < Items.MAX)
    else
      centered("SOLD", x, 560, 220, ui.f32, C.muted)
    end
  end
  centered("COIN REMOVAL", 1005, 532, 220, ui.f20, C.red)
  centered("REMOVE SELECTED COIN", 1005, 559, 220, ui.f16, C.muted)
  button("REMOVE / 8", 1016, 598, 198, 40, C.red,
    function() Game.remove(ui.game, ui.game.selected_uid) end,
    ui.game.player.gold >= 8 and #ui.game.coins > 1)

  text("YOUR DECK  /  " .. #ui.game.coins .. " OF " .. Game.DECK_MAX, 300, 657, ui.f20, C.gold)
  text(#ui.game.coins >= Game.DECK_MAX and "DECK FULL  -  REMOVE A COIN TO BUY ANOTHER" or
    "SELECT A COIN TO UPGRADE OR REMOVE", 706, 663, ui.f16, C.muted)
  for i = 1, Game.DECK_MAX do
    local x = 300 + (i - 1) * 94
    local item = ui.game.coins[i]
    local chosen = item and item.uid == ui.game.selected_uid
    box(x, 688, 88, 73, item and C.panel or C.slot)
    outline(x, 688, 88, 73, chosen and C.orange or C.panel_light)
    if item then
      coin_image(item.id, x + 20, 691, 48)
      centered(math.floor(Game.probability(ui.game, item) * 100 + .5) .. "% H", x, 742, 88, ui.f16, C.gold)
      coin_hover(item.id, x, 688, 88, 73, Game.probability(ui.game, item))
      ui.buttons[#ui.buttons + 1] = {x = x, y = 688, w = 88, h = 73,
        action = function() A.coin_action(item) end}
    else
      centered("EMPTY", x, 715, 88, ui.f16, C.muted)
    end
  end
end

return draw_shop
