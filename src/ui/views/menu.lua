-- Play screen: pick a character and one of its coin sets, then start. Sets are edited in the
-- Coin Sets screen.
local Game = require("src.game")
local Profile = require("src.profile")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_hover = D.coin_image, D.coin_hover
local characters = ui.characters

local ICON, GAP = 56, 12

local function arrow(label, x, delta)
  local mx, my = ui.mouse()
  local hover = mx >= x and mx <= x + 100 and my >= 300 and my <= 440
  box(x, 300 + (hover and -3 or 0), 100, 140, C.green)
  outline(x, 300 + (hover and -3 or 0), 100, 140, C.face)
  centered(label, x, 340 + (hover and -3 or 0), 100, ui.f48, C.ink)
  ui.buttons[#ui.buttons + 1] = {x = x, y = 300, w = 100, h = 140, action = function() A.cycle_character(delta) end}
end

local function draw_menu()
  box(144, 67, 992, 670, C.ink)
  outline(144, 67, 992, 670, C.gold)
  centered("TOSSUP", 160, 80, 960, ui.f48, C.gold)
  button("X", 164, 76, 60, 44, C.panel_light, function() A.go("title") end)

  arrow("<", 24, -1)
  arrow(">", 1156, 1)

  local character = characters[ui.selected_character]
  local active = Profile.active(ui.profile, ui.selected_character)
  local set = Profile.sets(ui.profile, ui.selected_character)[active]

  -- left: who you are
  box(172, 140, 346, 50, C.panel_light)
  centered(character.name:upper(), 172, 149, 346, ui.f32, C.face)
  box(172, 200, 346, 300, C.panel)
  outline(172, 200, 346, 300, C.panel_light)
  local portrait = ui.character_images[ui.selected_character]
  local scale = math.min(334 / portrait:getWidth(), 288 / portrait:getHeight())
  local width, height = portrait:getWidth() * scale, portrait:getHeight() * scale
  color(C.white)
  love.graphics.draw(portrait, 172 + (346 - width) / 2, 205 + (290 - height) / 2, 0, scale, scale)
  centered(character.description:upper(), 172, 512, 346, ui.f16, C.muted)
  centered("TOKENS  " .. ui.profile.tokens, 172, 540, 346, ui.f16, C.gold)

  -- right: the coin set you will play
  text("COIN SET", 560, 148, ui.f16, C.gold)
  button("<", 560, 170, 56, 44, C.panel_light, function() A.cycle_active_set(-1) end)
  box(626, 170, 300, 44, C.panel)
  centered(set.name .. "  /  " .. #A.loadout() .. " COINS", 626, 182, 300, ui.f20, C.face)
  button(">", 936, 170, 56, 44, C.panel_light, function() A.cycle_active_set(1) end)

  local coins = A.loadout()
  local columns = 5
  local x0 = 560 + (432 - (columns * (ICON + GAP) - GAP)) / 2
  for i = 1, Game.START_MAX do
    local x = x0 + ((i - 1) % columns) * (ICON + GAP)
    local y = 250 + math.floor((i - 1) / columns) * (ICON + GAP + 4)
    box(x - 4, y - 4, ICON + 8, ICON + 8, C.slot)
    outline(x - 4, y - 4, ICON + 8, ICON + 8, C.panel_light)
    if coins[i] then
      coin_image(coins[i], x, y, ICON)
      coin_hover(coins[i], x, y, ICON, ICON)
    end
  end
  if #set.coins == 0 then
    centered("THIS SET IS EMPTY  -  THE DEFAULT DECK IS USED", 560, 400, 432, ui.f16, C.orange)
  end
  button("EDIT COIN SETS", 626, 440, 300, 50, C.gold, function() A.open_sets(ui.selected_character) end)

  button("START RUN", 472, 666, 338, 55, C.blue, function() A.start() end)
end

return draw_menu
