-- Options: a Game tab (switches, language, clear progress) and a Sound tab (master / music / effects sliders).
local Lang = require("src.lang")
local Sound = require("src.ui.sound")
local Pad = require("src.ui.pad")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button

local ROWS = {
  {"screen_shake", "SCREEN SHAKE", "Shake the screen when a coin lands."},
  {"fast_flip", "FAST FLIP", "Half-length coin flip animation."},
  {"fullscreen", "FULLSCREEN", "Switch between windowed and fullscreen."},
}
local SLIDERS = {
  {"volume_master", "MASTER VOLUME", "Everything at once."},
  {"volume_music", "MUSIC", "The background loop."},
  {"volume_sfx", "SOUND EFFECTS", "Flips, scores, buttons."},
}

local function panel(y)
  box(280, y, 720, 78, C.panel_dk)
  outline(280, y, 720, 78, C.line)
end

-- A slider is a clickable strip: pressing or dragging sets the value from the mouse x; releasing saves it.
local function slider(key, label, hint, y)
  panel(y)
  text(label, 308, y + 10, ui.f32, C.face)
  text(hint, 308, y + 46, ui.f16, C.muted)
  local x, w = 600, 280
  local value = ui.profile.options[key]
  box(x, y + 30, w, 16, C.slot_dk, 8)
  color(C.gold)
  love.graphics.rectangle("fill", x, y + 30, w * value / 100, 16, 8)
  box(x + w * value / 100 - 8, y + 24, 16, 28, C.white, 4)
  text(value .. "%", 912, y + 24, ui.f32, C.gold)
  ui.buttons[#ui.buttons + 1] = {x = x - 10, y = y + 16, w = w + 20, h = 44, action = function() end,
    drag = function(mouse_x) A.set_volume(key, (mouse_x - x) / w * 100) end,
    adjust = function(direction) A.set_volume(key, ui.profile.options[key] + direction * 5) A.save_options() Sound.play("score") end, -- controller
    release = function() A.save_options() Sound.play("score") end}
end

-- Controls tab: what every key / button does, by context. {label, caps...}; a cap is a key name or {name, colour}.
local KEYBOARD = {
  {"MENUS"},
  {"Move the focus", "ARROWS"},
  {"Press the focused button", "ENTER"},
  {"Back, close, menu", "ESC"},
  {"Previous / next page, tab, character", "Q", "E"},
  {"Choose a character (play screen)", "1", "2", "3"},
  {"IN A LEVEL"},
  {"Flip / next coin", "SPACE"},
  {"Discard the coin in play", "D"},
  {"Use chip 1 / 2 / 3", "1", "2", "3"},
  {"Open the shop (quota met)", "O"},
  {"Debug info", "F3"},
}
local CONTROLLER = {
  {"MENUS"},
  {"Move the focus", "D-PAD", "STICK"},
  {"Press the focused button", {"A", "green"}},
  {"Back, close, menu", {"B", "red"}, "START"},
  {"Previous / next page, tab, character", "LB", "RB", "LT", "RT"},
  {"IN A LEVEL"},
  {"Flip / next coin", {"X", "blue"}},
  {"Discard the coin in play", {"Y", "gold"}},
  {"Use a chip or open the shop", "D-PAD", {"A", "green"}},
  {"Menu", "START"},
}

local function cap(name, x, y)
  local tint = C.panel_light
  if type(name) == "table" then name, tint = name[1], C[name[2]] end
  local w = ui.f16:getWidth(name) + 22
  box(x, y, w, 28, tint, 5)
  outline(x, y, w, 28, C.face, 5)
  text(name, x + 11, y + 6, ui.f16, C.ink)
  return w
end

local function draw_controls()
  local view = ui.controls_view or Pad.device
  button("KEYBOARD", 280, 198, 350, 40, view == "keyboard" and C.gold or C.panel_light, function() ui.controls_view = "keyboard" end)
  button("CONTROLLER", 650, 198, 350, 40, view == "controller" and C.gold or C.panel_light, function() ui.controls_view = "controller" end)
  local rows = view == "keyboard" and KEYBOARD or CONTROLLER
  local y = 252
  for _, row in ipairs(rows) do
    if #row == 1 then
      y = y + 6
      text(row[1], 290, y, ui.f20, C.gold)
      y = y + 30
    else
      text(row[1], 308, y + 6, ui.f16, C.face)
      local x = 700
      for i = 2, #row do x = x + cap(row[i], x, y) + 8 end
      y = y + 36
    end
  end
end

local function draw_options()
  D.frame(D.title("options"), "BACK", function() A.go("title") end)
  for i, tab in ipairs({{"game", "GAME"}, {"sound", "SOUND"}, {"controls", "CONTROLS"}}) do
    button(tab[2], 280 + (i - 1) * 245, 146, 230, 44, ui.options_tab == tab[1] and C.gold or C.panel_light, function() ui.options_tab = tab[1] end)
  end

  if ui.options_tab == "controls" then
    draw_controls()
  elseif ui.options_tab == "sound" then
    for i, row in ipairs(SLIDERS) do slider(row[1], row[2], row[3], 214 + (i - 1) * 96) end
    button("RESTORE DEFAULTS", 440, 510, 400, 48, C.panel_light, function() A.restore_sound_defaults() Sound.play("score") end)
  else
    for i, row in ipairs(ROWS) do
      local y = 214 + (i - 1) * 86
      local on = ui.profile.options[row[1]]
      panel(y)
      text(row[2], 308, y + 10, ui.f32, C.face)
      text(row[3], 308, y + 46, ui.f16, C.muted)
      button(on and "ON" or "OFF", 860, y + 16, 112, 46, on and C.green or C.panel_light, function() A.toggle_option(row[1]) end)
    end
    -- language: the button shows the current language's own name
    local y = 214 + #ROWS * 86
    panel(y)
    text("LANGUAGE", 308, y + 10, ui.f32, C.face)
    text("Language of all texts.", 308, y + 46, ui.f16, C.muted)
    button(Lang.names[Lang.current], 820, y + 16, 152, 46, C.gold, A.cycle_language)
    -- clear progress: opens a confirmation popup
    y = y + 86
    panel(y)
    text("CLEAR PROGRESS", 308, y + 10, ui.f32, C.face)
    text("Resets unlocks, collection, sets and tokens. Options stay.", 308, y + 46, ui.f16, C.muted)
    button("CLEAR", 820, y + 16, 152, 46, C.red, A.clear_progress)
  end
  if ui.options_tab ~= "controls" then centered("F3 SHOWS DEBUG INFO IN A RUN", 0, 715, 1280, ui.f16, C.muted) end
end

return draw_options
