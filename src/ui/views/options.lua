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
  box(344, 67, 592, 670, C.ink)
  outline(344, 67, 592, 670, C.gold)
  centered("OPTIONS", 344, 100, 592, ui.f48, C.gold)
  button("X", 364, 84, 60, 44, C.panel_light, function() A.go("title") end)
  for i, row in ipairs(ROWS) do
    local y = 220 + (i - 1) * 100
    local on = ui.profile.options[row[1]]
    box(384, y, 512, 76, C.panel)
    outline(384, y, 512, 76, C.panel_light)
    text(row[2], 404, y + 12, ui.f20, C.face)
    text(row[3], 404, y + 44, ui.f16, C.muted)
    button(on and "ON" or "OFF", 776, y + 16, 100, 44, on and C.green or C.panel_light,
      function() A.toggle_option(row[1]) end)
  end
  centered("F3 SHOWS DEBUG INFO IN A RUN", 344, 650, 592, ui.f16, C.muted)
end

return draw_options
