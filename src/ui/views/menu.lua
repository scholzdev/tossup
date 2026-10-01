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

local SLOT, GAP = 80, 22

local function draw_menu()
  D.frame(D.title("play"), "BACK", function() A.go("title") end)

  local character = characters[ui.selected_character]
  local active = Profile.active(ui.profile, ui.selected_character)
  local set = Profile.sets(ui.profile, ui.selected_character)[active]

  -- left: who you are, with arrows to change character
  button("<", 70, 160, 46, 48, C.green, function() A.cycle_character(-1) end)
  box(124, 160, 276, 48, C.panel_dk)
  outline(124, 160, 276, 48, C.line)
  centered(character.name:upper(), 124, 170, 276, ui.f32, C.face)
  button(">", 408, 160, 46, 48, C.green, function() A.cycle_character(1) end)
  box(70, 220, 384, 400, C.card)
  outline(70, 220, 384, 400, C.line)
  local portrait = ui.character_images[ui.selected_character]
  local scale = math.min(366 / portrait:getWidth(), 380 / portrait:getHeight())
  local width, height = portrait:getWidth() * scale, portrait:getHeight() * scale
  color(C.white)
  love.graphics.draw(portrait, 70 + (384 - width) / 2, 226 + (388 - height) / 2, 0, scale, scale)
  centered(character.description:upper(), 70, 632, 384, ui.f16, C.muted)

  -- right: the coin set you will play
  box(484, 160, 736, 460, C.panel_dk)
  outline(484, 160, 736, 460, C.line)
  centered("COIN SET", 484, 174, 736, ui.f16, C.gold)
  button("<", 510, 200, 50, 44, C.panel_light, function() A.cycle_active_set(-1) end)
  box(570, 200, 504, 44, C.card)
  centered(D.L("%s  /  %d COINS", set.name, #A.loadout()), 570, 210, 504, ui.f20, C.face)
  button(">", 1084, 200, 50, 44, C.panel_light, function() A.cycle_active_set(1) end)

  local coins = A.loadout()
  local columns = 5
  local x0 = 484 + (736 - (columns * (SLOT + GAP) - GAP)) / 2
  for i = 1, Game.START_MAX do
    local x = x0 + ((i - 1) % columns) * (SLOT + GAP)
    local y = 270 + math.floor((i - 1) / columns) * (SLOT + GAP + 4)
    box(x - 6, y - 6, SLOT + 12, SLOT + 12, C.slot_dk)
    outline(x - 6, y - 6, SLOT + 12, SLOT + 12, C.line)
    if coins[i] then
      coin_image(coins[i], x, y, SLOT)
      coin_hover(coins[i], x, y, SLOT, SLOT)
    end
  end
  if #set.coins == 0 then
    centered("THIS SET IS EMPTY  -  THE DEFAULT DECK IS USED", 484, 490, 736, ui.f16, C.orange)
  end
  button("EDIT COIN SETS", 674, 540, 356, 52, C.gold, function() A.open_sets(ui.selected_character) end)

  D.icon_button("START RUN", ui.ui_images.start_level, 470, 660, 340, 68, C.green, function() A.start() end)
end

return draw_menu
