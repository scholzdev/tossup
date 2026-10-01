local Lang = require("src.lang")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, box, outline, text, centered, button = D.C, D.box, D.outline, D.text, D.centered, D.button

local ROWS = {
  {"screen_shake", "SCREEN SHAKE", "Shake the screen when a coin lands."},
  {"fast_flip", "FAST FLIP", "Half-length coin flip animation."},
  {"fullscreen", "FULLSCREEN", "Switch between windowed and fullscreen."},
  {"sound", "SOUND", "Play sound effects."},
}

local function draw_options()
  D.frame(D.title("options"), "BACK", function() A.go("title") end)
  for i, row in ipairs(ROWS) do
    local y = 156 + (i - 1) * 86
    local on = ui.profile.options[row[1]]
    box(280, y, 720, 78, C.panel_dk)
    outline(280, y, 720, 78, C.line)
    text(row[2], 308, y + 10, ui.f32, C.face)
    text(row[3], 308, y + 46, ui.f16, C.muted)
    button(on and "ON" or "OFF", 860, y + 16, 112, 46, on and C.green or C.panel_light,
      function() A.toggle_option(row[1]) end)
  end
  -- language: the button shows the current language's own name
  local y = 156 + #ROWS * 86
  box(280, y, 720, 78, C.panel_dk)
  outline(280, y, 720, 78, C.line)
  text("LANGUAGE", 308, y + 10, ui.f32, C.face)
  text("Language of all texts.", 308, y + 46, ui.f16, C.muted)
  button(Lang.names[Lang.current], 820, y + 16, 152, 46, C.gold, A.cycle_language)
  -- clear progress: opens a confirmation popup
  y = y + 86
  box(280, y, 720, 78, C.panel_dk)
  outline(280, y, 720, 78, C.line)
  text("CLEAR PROGRESS", 308, y + 10, ui.f32, C.face)
  text("Resets unlocks, collection, sets and tokens. Options stay.", 308, y + 46, ui.f16, C.muted)
  button("CLEAR", 820, y + 16, 152, 46, C.red, A.clear_progress)
  centered("F3 SHOWS DEBUG INFO IN A RUN", 0, 715, 1280, ui.f16, C.muted)
end

return draw_options
