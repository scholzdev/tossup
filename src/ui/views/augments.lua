local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, box, outline, centered, button = D.C, D.box, D.outline, D.centered, D.button

local function draw_augments()
  local g = ui.game
  box(0, 0, 1280, 800, C.felt_dark)
  box(36, 36, 1208, 728, C.screen)
  outline(36, 36, 1208, 728, C.gold)

  centered(D.L("LEVEL %d AUGMENT", g.augment_level), 70, 82, 1140, ui.f32, C.gold)
  centered(D.L("Choose an upgrade that stays active for the rest of this run."),
    70, 128, 1140, ui.f20, C.muted)

  local width, height, gap = 300, 390, 30
  local start_x = (1280 - (width * 3 + gap * 2)) / 2
  for i, id in ipairs(g.augment_options or {}) do
    local def = Game.augment_def(id)
    if def then
      local x, y = start_x + (i - 1) * (width + gap), 205
      box(x, y, width, height, C.panel_dk)
      outline(x, y, width, height, C.line)
      centered(D.L(def.name), x + 16, y + 26, width - 32, ui.f20, C.gold)
      local image = ui.augment_images[id]
      if image then D.image_at(image, x + (width - 96) / 2, y + 70, 96) end
      centered(D.L(def.tier:upper() .. " TIER"), x + 16, y + 174, width - 32, ui.f16, C.muted)
      love.graphics.setFont(ui.f16)
      D.color(C.face)
      love.graphics.printf(D.L(def.description), x + 28, y + 212, width - 56, "center")
      button(D.L("CHOOSE AUGMENT"), x + 24, y + 326, width - 48, 48, C.blue,
        function() A.choose_augment(id) end, true)
    end
  end
end

return draw_augments
