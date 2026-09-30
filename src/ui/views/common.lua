local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_face, coin_hover, effects, effect_description =
  D.coin_image, D.coin_face, D.coin_hover, D.effects, D.effect_description
local catalog, characters = ui.catalog, ui.characters

local function sidebar()
  box(16, 16, 248, 768, C.ink)
  outline(16, 16, 248, 768, C.panel_light)
  centered("TOSSUP", 25, 40, 230, ui.f48, C.gold)
  local p = ui.game.player
  local stats = {
    {"GOLD", tostring(p.gold), C.gold},
    {"ENERGY", tostring(p.energy), C.blue},
  }
  for i, entry in ipairs(stats) do
    local y = 146 + (i - 1) * 75
    box(30, y, 220, 63, C.panel)
    text(entry[1], 41, y + 5, ui.f16, C.muted)
    text(entry[2], 41, y + 25, ui.f32, entry[3])
  end
  text("RUN  /  " .. ui.game.encounter_index .. " OF 4", 31, 385, ui.f20, C.face)
  text("SEED " .. ui.game.seed, 31, 417, ui.f16, C.muted)
  box(29, 456, 222, 244, C.slot)
  local portrait = ui.character_images[ui.game.character_id]
  local scale = math.min(210 / portrait:getWidth(), 194 / portrait:getHeight())
  local width, height = portrait:getWidth() * scale, portrait:getHeight() * scale
  color(C.white)
  love.graphics.draw(portrait, 35 + (210 - width) / 2, 462 + (194 - height) / 2,
    0, scale, scale)
  centered(characters[ui.game.character_id].name:upper(), 35, 663, 210, ui.f20, C.gold)
  button("MENU / ESC", 30, 724, 220, 43, C.panel_light, function() ui.game.paused = true end)
end

return {sidebar = sidebar}
