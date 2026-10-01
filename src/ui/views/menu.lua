local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_hover = D.coin_image, D.coin_hover
local characters = ui.characters

local ICON, GAP, COLUMNS = 52, 10, 9
local GRID_X = 566

local function arrow(label, x, delta)
  local mx, my = ui.mouse()
  local hover = mx >= x and mx <= x + 100 and my >= 300 and my <= 440
  box(x, 300 + (hover and -3 or 0), 100, 140, C.green)
  outline(x, 300 + (hover and -3 or 0), 100, 140, C.face)
  centered(label, x, 340 + (hover and -3 or 0), 100, ui.f48, C.ink)
  ui.buttons[#ui.buttons + 1] = {x = x, y = 300, w = 100, h = 140, action = function() A.cycle_character(delta) end}
end

local function padlock(cx, cy)
  color(C.face)
  love.graphics.setLineWidth(3)
  love.graphics.arc("line", "open", cx, cy - 2, 6, math.pi, 2 * math.pi)
  love.graphics.setLineWidth(1)
  love.graphics.rectangle("fill", cx - 9, cy - 2, 18, 14, 2)
  color(C.ink)
  love.graphics.circle("fill", cx, cy + 5, 2)
end

-- Draw coin icons in rows. Locked entries get a padlock, their token cost, and unlock on click.
local function grid(entries, y)
  local rows = math.max(1, math.ceil(#entries / COLUMNS))
  local row_height = ICON + GAP + 16
  box(GRID_X - 8, y, COLUMNS * (ICON + GAP) + 6, rows * row_height + 10, C.slot)
  for i, entry in ipairs(entries) do
    local x = GRID_X + ((i - 1) % COLUMNS) * (ICON + GAP)
    local top = y + 8 + math.floor((i - 1) / COLUMNS) * row_height
    coin_image(entry.id, x, top, ICON)
    if entry.locked then
      color(C.slot, .7)
      love.graphics.circle("fill", x + ICON / 2, top + ICON / 2, ICON / 2)
      padlock(x + ICON / 2, top + ICON / 2 - 4)
      centered(tostring(entry.cost), x, top + ICON + 1, ICON, ui.f16,
        ui.profile.tokens >= entry.cost and C.gold or C.muted)
      ui.buttons[#ui.buttons + 1] = {x = x, y = top, w = ICON, h = ICON,
        action = function() A.unlock_coin(entry.id) end}
    end
    coin_hover(entry.id, x, top, ICON, ICON, nil, entry.locked and entry.cost or nil)
  end
  return rows * row_height + 10
end

local function draw_menu()
  box(144, 67, 992, 670, C.ink)
  outline(144, 67, 992, 670, C.gold)
  centered("TOSSUP", 160, 80, 960, ui.f48, C.gold)

  arrow("<", 24, -1)
  arrow(">", 1156, 1)

  -- left: who you are
  local character = characters[ui.selected_character]
  box(172, 140, 346, 50, C.panel_light)
  centered(character.name:upper(), 172, 149, 346, ui.f32, C.face)
  box(172, 200, 346, 250, C.panel)
  outline(172, 200, 346, 250, C.panel_light)
  local portrait = ui.character_images[ui.selected_character]
  local scale = math.min(334 / portrait:getWidth(), 238 / portrait:getHeight())
  local width, height = portrait:getWidth() * scale, portrait:getHeight() * scale
  color(C.white)
  love.graphics.draw(portrait, 172 + (346 - width) / 2, 205 + (240 - height) / 2, 0, scale, scale)
  centered(character.description:upper(), 172, 462, 346, ui.f16, C.muted)
  centered("STARTING DECK", 172, 500, 346, ui.f16, C.gold)
  local deck = character.deck or {character.starter}
  local x0 = 172 + (346 - (#deck * (ICON + GAP) - GAP)) / 2
  for i, id in ipairs(deck) do
    local x = x0 + (i - 1) * (ICON + GAP)
    box(x - 4, 524, ICON + 8, ICON + 8, C.slot)
    coin_image(id, x, 528, ICON)
    coin_hover(id, x, 528, ICON, ICON)
  end

  -- right: what you can find
  local available, locked = {}, {}
  for _, entry in ipairs(A.menu_coins(ui.selected_character)) do
    if entry.locked then locked[#locked + 1] = entry else available[#available + 1] = entry end
  end
  text("IN THE SHOP", GRID_X - 8, 148, ui.f16, C.gold)
  centered("TOKENS  " .. ui.profile.tokens, 850, 148, 252, ui.f16, C.gold)
  local used = grid(available, 170)
  local top = 170 + used + 22
  text("LOCKED  /  SPEND TOKENS IN THE MENU", GRID_X - 8, top, ui.f16, C.gold)
  grid(locked, top + 22)

  button("X", 164, 76, 60, 44, C.panel_light, function() A.go("title") end)
  button("START RUN", 472, 666, 338, 55, C.blue, function() A.start() end)
end

return draw_menu
