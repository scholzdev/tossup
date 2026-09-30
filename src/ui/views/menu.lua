local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_name, coin_face, coin_hover, effects, effect_description =
  D.coin_image, D.coin_name, D.coin_face, D.coin_hover, D.effects, D.effect_description
local catalog, characters = ui.catalog, ui.characters

local function draw_menu()
  box(144, 67, 992, 670, C.ink)
  outline(144, 67, 992, 670, C.gold)
  centered("COIN ROGUELIKE", 160, 92, 960, ui.f48, C.gold)
  local character = characters[ui.selected_character]
  text("CHARACTER", 173, 186, ui.f16, C.gold)
  box(172, 210, 346, 310, C.panel)
  outline(172, 210, 346, 310, C.panel_light)
  local portrait = ui.character_images[ui.selected_character]
  local scale = math.min(334 / portrait:getWidth(), 300 / portrait:getHeight())
  local width, height = portrait:getWidth() * scale, portrait:getHeight() * scale
  color(C.white)
  love.graphics.draw(portrait, 172 + (346 - width) / 2, 215 + (300 - height) / 2, 0, scale, scale)
  centered(character.name:upper(), 172, 527, 346, ui.f32, C.face)
  centered(character.description:upper(), 172, 566, 346, ui.f16, C.muted)
  for i, id in ipairs(ui.character_order) do
    local x = 172 + (i - 1) * 118
    button(characters[id].name:sub(5):upper(), x, 601, 110, 43,
      id == ui.selected_character and C.gold or C.panel_light,
      function() A.select_character(id) end)
  end

  local coins = A.menu_coins(ui.selected_character)
  text("COINS  /  " .. #coins, 550, 186, ui.f16, C.gold)
  centered("TOKENS  " .. ui.profile.tokens, 870, 186, 224, ui.f16, C.gold)
  local first = (ui.coin_page - 1) * ui.coins_per_page + 1
  for slot = 1, ui.coins_per_page do
    local entry = coins[first + slot - 1]
    if entry then
      local id = entry.id
      local x = 550 + ((slot - 1) % 2) * 279
      local y = 210 + math.floor((slot - 1) / 2) * 181
      box(x, y, 265, 169, C.panel)
      outline(x, y, 265, 169, C.panel_light)
      if entry.locked then
        coin_image(id, x + 84, y + 8, 96)
        color(C.panel, .75)
        love.graphics.circle("fill", x + 132, y + 56, 50)
        coin_name(id, x, y + 106, 265)
        button("UNLOCK / " .. entry.cost, x + 52, y + 130, 161, 31, C.gold,
          function() A.unlock_coin(id) end, ui.profile.tokens >= entry.cost)
      else
        coin_image(id, x + 66, y + 14, 133)
        coin_name(id, x + 66, y + 140, 133)
      end
      coin_hover(id, x, y, 265, entry.locked and 128 or 169)
    end
  end
  local pages = math.ceil(#coins / ui.coins_per_page)
  button("<", 665, 582, 57, 45, C.panel_light, function() A.change_coin_page(-1) end,
    ui.coin_page > 1)
  centered("PAGE " .. ui.coin_page .. " / " .. pages, 736, 592, 172, ui.f20, C.face)
  button(">", 922, 582, 57, 45, C.panel_light, function() A.change_coin_page(1) end,
    ui.coin_page < pages)
  if ui.game then
    button("CONTINUE", 282, 666, 338, 55, C.blue, function() ui.game.paused = false end)
    button("NEW RUN", 660, 666, 338, 55, C.gold, function() A.start() end)
  else
    button("START RUN", 472, 666, 338, 55, C.blue, function() A.start() end)
  end
end

return draw_menu
