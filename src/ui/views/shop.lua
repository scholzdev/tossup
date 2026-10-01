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

local SCREEN = {.09, .27, .30}
local LETTER_COLORS = {C.orange, C.red, C.blue, C.green}

local function vertical_label(word, x, y)
  for i = 1, #word do
    centered(i == 1 and word:sub(i, i):upper() or word:sub(i, i):lower(), x, y + (i - 1) * 30, 30, ui.f32, C.white)
  end
end

local function price(amount, x, y, w, affordable)
  centered(tostring(amount), x, y, w, ui.f32, affordable and C.gold or C.red)
end

-- a square plate for items and relics (they have no coin art): short name inside
local function plate(label, x, y, size, tint)
  box(x, y, size, size, tint)
  outline(x, y, size, size, C.panel_light)
  centered(label, x, y + size / 2 - 10, size, ui.f20, C.ink)
end

local function draw_shop()
  local g = ui.game
  box(0, 0, 1280, 800, C.felt_dark)
  box(36, 36, 1208, 728, SCREEN)
  outline(36, 36, 1208, 728, C.gold)

  -- title, gold, menu
  love.graphics.setFont(ui.f48)
  for i, letter in ipairs({"S", "H", "O", "P"}) do
    color(LETTER_COLORS[i])
    love.graphics.print(letter, 80 + (i - 1) * 62, 56, 0, 2, 2)
  end
  text("GOLD", 1030, 56, ui.f20, C.muted)
  centered(tostring(g.player.gold), 990, 78, 220, ui.f48, C.gold)
  button("MENU", 1124, 140, 92, 30, C.panel_light, A.open_menu)

  -- reroll (coin offers only)
  local reroll_cost = g.reroll_cost or 4
  box(60, 260, 190, 150, {.06, .20, .23})
  outline(60, 260, 190, 150, C.panel_light)
  centered("REROLL", 60, 272, 190, ui.f20, C.muted)
  price(reroll_cost, 60, 300, 190, g.player.gold >= reroll_cost)
  button("REROLL", 80, 352, 150, 40, C.orange, function() Game.reroll_shop(g) end, g.player.gold >= reroll_cost)

  -- COIN row
  local full = #g.coins >= Game.DECK_MAX
  vertical_label("Coin", 262, 232)
  for i = 1, 4 do
    local x = 330 + (i - 1) * 150
    local id = g.shop_offers[i]
    if id then
      local cost = catalog[id].cost or 15
      price(cost, x, 222, 110, g.player.gold >= cost and not full)
      coin_image(id, x + 7, 262, 96)
      coin_hover(id, x, 262, 110, 96)
      button(full and "FULL" or "BUY", x, 370, 110, 34, C.blue, function() Game.buy(g, i) end,
        g.player.gold >= cost and not full)
    else
      centered("SOLD", x, 300, 110, ui.f32, C.muted)
    end
  end

  -- CHIP row (items)
  vertical_label("Chip", 262, 452)
  for i = 1, 2 do
    local x = 330 + (i - 1) * 150
    local id = g.shop_items[i]
    if id then
      local def = ui.item_catalog[id]
      price(def.cost, x, 442, 110, g.player.gold >= def.cost and #g.items < Items.MAX)
      plate(def.short, x + 7, 480, 96, C.orange)
      D.text_hover(def.name, def.description, x + 7, 480, 96, 96)
      button("BUY", x, 588, 110, 34, C.blue, function() Game.buy_item(g, i) end,
        g.player.gold >= def.cost and #g.items < Items.MAX)
    else
      centered("SOLD", x, 520, 110, ui.f32, C.muted)
    end
  end

  -- PRIZE row (relic)
  vertical_label("Prize", 660, 442)
  local relic = g.shop_relic and Game.relics()[g.shop_relic]
  if relic then
    local x = 720
    price(25, x, 442, 110, g.player.gold >= 25)
    plate(relic.name:sub(1, 7):upper(), x + 7, 480, 96, C.gold)
    D.text_hover(relic.name, relic.description, x + 7, 480, 96, 96)
    button("BUY", x, 588, 110, 34, C.blue, function() Game.buy_relic(g) end, g.player.gold >= 25)
  else
    centered("SOLD", 720, 520, 110, ui.f32, C.muted)
  end

  -- tune-ups for the selected coin
  box(1000, 232, 216, 392, {.06, .20, .23})
  outline(1000, 232, 216, 392, C.panel_light)
  centered("TUNE-UPS", 1000, 244, 216, ui.f20, C.gold)
  local selected = Game.get_coin(g, g.selected_uid)
  centered("ODDS TUNER", 1000, 290, 216, ui.f20, C.face)
  centered("+10% HEADS ON THE", 1000, 318, 216, ui.f16, C.muted)
  centered("SELECTED COIN", 1000, 338, 216, ui.f16, C.muted)
  price(10, 1000, 362, 216, g.player.gold >= 10)
  button("UPGRADE", 1030, 410, 156, 36, C.gold, function() Game.upgrade(g, g.selected_uid) end,
    g.player.gold >= 10 and selected and Game.probability(g, selected) < 1)
  centered("COIN REMOVAL", 1000, 480, 216, ui.f20, C.face)
  centered("DROP THE SELECTED COIN", 1000, 508, 216, ui.f16, C.muted)
  price(8, 1000, 530, 216, g.player.gold >= 8)
  button("REMOVE", 1030, 578, 156, 36, C.red, function() Game.remove(g, g.selected_uid) end,
    g.player.gold >= 8 and #g.coins > 1)

  -- your deck
  text("YOUR DECK  " .. #g.coins .. " / " .. Game.DECK_MAX, 60, 640, ui.f20, C.gold)
  text(full and "DECK FULL  -  REMOVE A COIN TO BUY ANOTHER" or "CLICK A COIN TO SELECT IT", 330, 646, ui.f16, full and C.orange or C.muted)
  for i = 1, Game.DECK_MAX do
    local x = 60 + (i - 1) * 92
    local item = g.coins[i]
    local chosen = item and item.uid == g.selected_uid
    box(x, 672, 84, 76, item and {.06, .20, .23} or C.slot)
    outline(x, 672, 84, 76, chosen and C.orange or C.panel_light)
    if item then
      coin_image(item.id, x + 18, 676, 48)
      centered(math.floor(Game.probability(g, item) * 100 + .5) .. "% H", x, 726, 84, ui.f16, C.gold)
      coin_hover(item.id, x, 672, 84, 76, Game.probability(g, item))
      ui.buttons[#ui.buttons + 1] = {x = x, y = 672, w = 84, h = 76, action = function() A.coin_action(item) end}
    end
  end

  -- held chips and prizes
  local held = {}
  for i, id in ipairs(g.items) do held[i] = ui.item_catalog[id].short end
  for _, id in ipairs(g.relics) do held[#held + 1] = Game.relics()[id].name:upper() end
  if #held > 0 then text("HELD  " .. table.concat(held, ", "), 330, 626, ui.f16, C.orange) end

  -- next round: the big red button
  local mx, my = ui.mouse()
  local over = (mx - 1130) ^ 2 + (my - 690) ^ 2 <= 62 ^ 2
  color(C.black, .4)
  love.graphics.circle("fill", 1130, 698, 62)
  color(over and {1, .38, .33} or {.85, .18, .16})
  love.graphics.circle("fill", 1130, 690 + (over and -2 or 0), 62)
  color(C.white)
  love.graphics.setLineWidth(3)
  love.graphics.circle("line", 1130, 690 + (over and -2 or 0), 62)
  love.graphics.setLineWidth(1)
  centered("NEXT", 1068, 664, 124, ui.f32, C.white)
  centered("ROUND", 1068, 694, 124, ui.f20, C.white)
  ui.buttons[#ui.buttons + 1] = {x = 1068, y = 628, w = 124, h = 124, action = function() Game.leave_shop(g) end}
end

return draw_shop
