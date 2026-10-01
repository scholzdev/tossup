local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, box, outline, text, centered, button = D.C, D.box, D.outline, D.text, D.centered, D.button

local ROWS = {
  {"screen_shake", "SCREEN SHAKE", "Shake the screen when a coin lands."},
  {"fast_flip", "FAST FLIP", "Half-length coin flip animation."},
  {"fullscreen", "FULLSCREEN", "Switch between windowed and fullscreen."},
}

local function draw_options()
  D.frame(ui.ui_images.title_options, "BACK", function() A.go("title") end)
  for i, row in ipairs(ROWS) do
    local y = 190 + (i - 1) * 110
    local on = ui.profile.options[row[1]]
    box(280, y, 720, 90, C.panel_dk)
    outline(280, y, 720, 90, C.line)
    text(row[2], 308, y + 16, ui.f32, C.face)
    text(row[3], 308, y + 56, ui.f16, C.muted)
    button(on and "ON" or "OFF", 860, y + 22, 112, 46, on and C.green or C.panel_light,
      function() A.toggle_option(row[1]) end)
  end
  centered("F3 SHOWS DEBUG INFO IN A RUN", 0, 690, 1280, ui.f16, C.muted)
end

return draw_options
