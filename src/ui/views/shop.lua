-- Shop (full screen): coins, chips (items) and prizes (relics), a reroll for the coin offers,
-- and tune-ups for the coin you select in the deck strip. Buying a locked coin unlocks it for good.
local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local Items = require("src.items")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_hover = D.coin_image, D.coin_hover
local catalog = ui.catalog
local Lang = require("src.lang")

local SCREEN = {.09, .27, .30}

local function vertical_label(word, x, y)
  for i, ch in ipairs(Lang.chars(D.L(word))) do
    centered(i == 1 and ch:upper() or ch, x, y + (i - 1) * 30, 30, ui.f32, C.white)
  end
end

local function price(amount, x, y, w, affordable)
  centered(tostring(amount), x, y, w, ui.f32, affordable and C.gold or C.red)
end

-- Grid: offer columns start at X0 with a fixed step, so every row lines up with the coin row.
local X0, STEP = 340, 150
local PANEL = {.06, .20, .23}

local function draw_shop()
  local g = ui.game
  box(0, 0, 1280, 800, C.felt_dark)
  box(36, 36, 1208, 728, SCREEN)
  outline(36, 36, 1208, 728, C.gold)

  -- title, gold, menu
  color(C.white)
  love.graphics.draw(D.title("shop"), 70, 44, 0, 110 / D.title("shop"):getHeight(),
    110 / D.title("shop"):getHeight())
  button("MENU", 1120, 56, 100, 34, C.panel_light, A.open_menu)
  D.image_at(ui.ui_images.gold, 1010, 100, 44)
  text(tostring(g.player.gold), 1062, 104, ui.f32, C.gold)

  -- reroll (coin offers only), level with the coin icons
  local reroll_cost = g.reroll_cost or 4
  box(70, 228, 190, 140, PANEL)
  outline(70, 228, 190, 140, C.panel_light)
  D.image_at(ui.ui_images.reroll, 82, 238, 34)
  text("REROLL", 124, 245, ui.f20, C.muted)
  price(reroll_cost, 70, 274, 190, g.player.gold >= reroll_cost)
  button("REROLL", 90, 322, 150, 36, C.orange, function() Game.reroll_shop(g) end, g.player.gold >= reroll_cost)

  -- COIN row
  local full = #g.coins >= Game.DECK_MAX
  vertical_label("Coin", 290, 232)
  for i = 1, 4 do
    local x = X0 + (i - 1) * STEP
    local id = g.shop_offers[i]
    if id then
      local cost = catalog[id].cost or 15
      price(cost, x, 186, 110, g.player.gold >= cost and not full)
      coin_image(id, x + 7, 228, 96)
      coin_hover(id, x, 228, 110, 96)
      button(full and "FULL" or "BUY", x, 336, 110, 34, C.blue, function() Game.buy(g, i) end,
        g.player.gold >= cost and not full)
    else
      centered("SOLD", x, 268, 110, ui.f32, C.muted)
    end
  end

  -- CHIP row (items), columns 1-2
  vertical_label("Chip", 290, 436)
  for i = 1, 2 do
    local x = X0 + (i - 1) * STEP
    local id = g.shop_items[i]
    if id then
      local def = ui.item_catalog[id]
      price(def.cost, x, 392, 110, g.player.gold >= def.cost and #g.items < Items.MAX)
      D.image_at(ui.item_images[id], x + 7, 434, 96)
      D.text_hover(def.name, def.description, x + 7, 434, 96, 96)
      button("BUY", x, 542, 110, 34, C.blue, function() Game.buy_item(g, i) end,
        g.player.gold >= def.cost and #g.items < Items.MAX)
    else
      centered("SOLD", x, 474, 110, ui.f32, C.muted)
    end
  end

  -- PRIZE row (relic), column 4 so it lines up with the last coin offer
  local px = X0 + 3 * STEP
  vertical_label("Prize", px - 50, 396)
  local relic = g.shop_relic and ui.relic_catalog[g.shop_relic]
  if relic then
    price(25, px, 392, 110, g.player.gold >= 25)
    D.image_at(ui.relic_images[g.shop_relic], px + 7, 434, 96)
    D.text_hover(relic.name, relic.description, px + 7, 434, 96, 96)
    button("BUY", px, 542, 110, 34, C.blue, function() Game.buy_relic(g) end, g.player.gold >= 25)
  else
    centered("SOLD", px, 474, 110, ui.f32, C.muted)
  end

  -- tune-ups for the selected coin
  box(1000, 190, 220, 370, PANEL)
  outline(1000, 190, 220, 370, C.line)
  centered("TUNE-UPS", 1000, 200, 220, ui.f20, C.gold)
  local selected = Game.get_coin(g, g.selected_uid)
  centered("ODDS TUNER", 1000, 236, 220, ui.f20, C.face)
  centered("+10% HEADS ON THE", 1000, 262, 220, ui.f16, C.muted)
  centered("SELECTED COIN", 1000, 281, 220, ui.f16, C.muted)
  price(10, 1000, 298, 220, g.player.gold >= 10)
  button("UPGRADE", 1032, 340, 156, 36, C.gold, function() Game.upgrade(g, g.selected_uid) end,
    g.player.gold >= 10 and selected and Game.probability(g, selected) < 1)
  centered("COIN REMOVAL", 1000, 400, 220, ui.f20, C.face)
  centered("DROP THE SELECTED COIN", 1000, 426, 220, ui.f16, C.muted)
  price(8, 1000, 444, 220, g.player.gold >= 8)
  button("REMOVE", 1032, 486, 156, 36, C.red, function() Game.remove(g, g.selected_uid) end,
    g.player.gold >= 8 and #g.coins > 1)

  -- your deck
  text(D.L("YOUR DECK  %d / %d", #g.coins, Game.DECK_MAX), 70, 612, ui.f20, C.gold)
  text(full and "DECK FULL  -  REMOVE A COIN TO BUY ANOTHER" or "CLICK A COIN TO SELECT IT", 340, 618, ui.f16,
    full and C.orange or C.muted)
  local held = {}
  for i, id in ipairs(g.items) do held[i] = ui.item_catalog[id].short end
  for _, id in ipairs(g.relics) do held[#held + 1] = ui.relic_catalog[id].name:upper() end
  if #held > 0 then text(D.L("HELD  %s", table.concat(held, ", ")), 340, 638, ui.f16, C.orange) end
  for i = 1, Game.DECK_MAX do
    local x = 70 + (i - 1) * 92
    local item = g.coins[i]
    local chosen = item and item.uid == g.selected_uid
    box(x, 664, 84, 76, item and PANEL or C.slot)
    outline(x, 664, 84, 76, chosen and C.orange or C.panel_light)
    if item then
      coin_image(item.id, x + 18, 668, 48)
      centered(D.L("%d%% H", math.floor(Game.probability(g, item) * 100 + .5)), x, 718, 84, ui.f16, C.gold)
      coin_hover(item.id, x, 664, 84, 76, Game.probability(g, item))
      ui.buttons[#ui.buttons + 1] = {x = x, y = 664, w = 84, h = 76, action = function() A.coin_action(item) end}
    end
  end

  -- next round: the big red button, under the tune-ups
  local mx, my = ui.mouse()
  local over = mx >= 1054 and mx <= 1166 and my >= 586 and my <= 698
  D.image_at(ui.ui_images.next_round, 1054, 586 + (over and -3 or 0), 112)
  centered("NEXT ROUND", 1000, 706, 220, ui.f20, C.white)
  ui.buttons[#ui.buttons + 1] = {x = 1054, y = 586, w = 112, h = 144, action = function() Game.leave_shop(g) end}
end

return draw_shop
