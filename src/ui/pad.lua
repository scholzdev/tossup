-- Controller support. The D-pad or left stick moves a focus between the clickable buttons of the current screen (the same
-- list the mouse uses), A presses the focused button, B / Start = Esc, X = Space (flip / next coin), Y = discard the coin in play,
-- the shoulder buttons = Left / Right (pages, characters). Sliders: left / right changes the focused slider.
-- The focus ring only shows after a controller was used; moving the mouse hides it again.
local ui = require("src.ui.state")
local Game = require("src.game")

local Pad = {device = "keyboard", active = false, focus = nil, key = nil, repeat_timer = 0, trigger_down = {}}

local DIRECTIONS = {dpup = {0, -1}, dpdown = {0, 1}, dpleft = {-1, 0}, dpright = {1, 0}}

local function center(b) return b.x + b.w / 2, b.y + b.h / 2 end

-- Where the focus starts on a screen: the main action.
local function default_focus()
  local game = ui.game
  if ui.confirm then return ui.confirm.single and 640 or 760, 480 end -- Cancel, never the destructive OK
  if ui.tutorial then return 640, 400 end
  if game and not game.paused then
    if game.phase == "ENCOUNTER" then return 680, 708 end
    if game.phase == "SHOP" then return 1110, 658 end
    return 640, 614 -- run over: the first button
  end
  if ui.screen == "select" then return 640, 694 end -- Start Run
  local first = ui.buttons[1]
  if first then return center(first) end
  return 640, 400
end

local function context_key()
  local game = ui.game
  return table.concat({ui.screen, game and not game.paused and game.phase or "-", game and game.mulligan and "m" or "-",
    ui.confirm and "c" or "-", ui.tutorial and ui.tutorial.step or "-"}, ":")
end

-- The button nearest to a point (and how far away it is).
local function nearest(px, py)
  local best, best_d
  for _, b in ipairs(ui.buttons) do
    local cx, cy = center(b)
    local d = (cx - px) ^ 2 + (cy - py) ^ 2
    if not best_d or d < best_d then best, best_d = b, d end
  end
  return best, best_d
end

-- The button the focus is on. If it is unavailable right now (the Flip button during the flip animation is not clickable),
-- there is no focus: no ring, and A does nothing, instead of jumping to some other button such as Menu.
function Pad.focused()
  if not Pad.focus then return nil end
  local best, d = nearest(Pad.focus[1], Pad.focus[2])
  if best and d <= 120 ^ 2 then return best end
end

local function move(dx, dy)
  local current = Pad.focused()
  if not current then -- the focused button is gone: the first press picks the nearest button
    local best = Pad.focus and nearest(Pad.focus[1], Pad.focus[2])
    if best then Pad.focus = {center(best)} end
    return
  end
  local fx, fy = center(current)
  local best, best_score
  for _, b in ipairs(ui.buttons) do
    if b ~= current then
      local cx, cy = center(b)
      local rx, ry = cx - fx, cy - fy
      local along = rx * dx + ry * dy
      if along > 1 then
        local score = along + 2.5 * math.abs(rx * dy - ry * dx) -- prefer the button most in line with the direction
        if not best_score or score < best_score then best, best_score = b, score end
      end
    end
  end
  if not best then
    -- nothing lies in that direction (a vertical menu has no left / right): step through the buttons in reading order instead,
    -- and up / down wrap around to the other end
    local order = {}
    for _, b in ipairs(ui.buttons) do order[#order + 1] = b end
    table.sort(order, function(a, b)
      if math.abs(a.y - b.y) > 12 then return a.y < b.y end
      return a.x < b.x
    end)
    for i, b in ipairs(order) do
      if b == current then
        local step = (dx + dy) > 0 and 1 or -1
        best = order[(i - 1 + step) % #order + 1]
        break
      end
    end
  end
  if best then Pad.focus = {center(best)} end
end

-- LB / RB / LT / RT: the previous / next page, tab or character of the current screen.
function Pad.step(direction, app)
  Pad.active = true
  if ui.confirm or ui.tutorial then return end
  if ui.game and not ui.game.paused then return end
  local screen = ui.screen
  if screen == "select" then app.cycle_character(direction)
  elseif screen == "collection" then app.change_collection_page(direction)
  elseif screen == "options" then
    local tabs = {"game", "sound", "controls"}
    for i, tab in ipairs(tabs) do
      if tab == ui.options_tab then ui.options_tab = tabs[(i - 1 + direction) % #tabs + 1] break end
    end
  elseif screen == "sets" then app.cycle_sets_character(direction) end
end

-- Call every frame after the UI was drawn (the button list is then complete).
function Pad.update(dt, app)
  local key = context_key()
  if key ~= Pad.key then -- a new screen: pick the start focus one frame later, when its buttons exist
    Pad.key = key
    Pad.need_default = true
  elseif Pad.need_default then
    Pad.need_default = false
    local x, y = default_focus()
    Pad.focus = {x, y}
  end
  -- the left stick of the first connected gamepad works like the D-pad, with key repeat
  Pad.repeat_timer = math.max(0, Pad.repeat_timer - dt)
  local pad = love.joystick and love.joystick.getJoysticks()[1]
  if pad and pad:isGamepad() and Pad.repeat_timer == 0 then
    local ax, ay = pad:getGamepadAxis("leftx"), pad:getGamepadAxis("lefty")
    if math.abs(ax) > .6 or math.abs(ay) > .6 then
      Pad.pressed(math.abs(ax) > math.abs(ay) and (ax > 0 and "dpright" or "dpleft") or (ay > 0 and "dpdown" or "dpup"), app)
      Pad.repeat_timer = .22
    end
  end
  -- the triggers are analog axes, not buttons: a press is the moment they pass half way
  if pad and pad:isGamepad() then
    for name, direction in pairs({triggerleft = -1, triggerright = 1}) do
      local down = pad:getGamepadAxis(name) > .5
      if down and not Pad.trigger_down[name] then Pad.step(direction, app) end
      Pad.trigger_down[name] = down
    end
  end
end

function Pad.pressed(button, app)
  Pad.active = true
  local direction = DIRECTIONS[button]
  if direction then
    local current = Pad.focused()
    if current and current.adjust and direction[2] == 0 then current.adjust(direction[1]) return end
    move(direction[1], direction[2])
  elseif button == "a" then
    local current = Pad.focused()
    if current then app.activate(current) end
  elseif button == "b" or button == "start" then
    app.keypressed("escape")
  elseif button == "x" then
    app.keypressed("space")
  elseif button == "y" then
    local game = ui.game
    if game and not game.paused and game.phase == "ENCOUNTER" and game.dealt and not ui.flip_animation and not ui.holding
      and not game.mulligan and not ui.tutorial and not ui.confirm then
      app.discard_current()
    end
  elseif button == "leftshoulder" then
    Pad.step(-1, app)
  elseif button == "rightshoulder" then
    Pad.step(1, app)
  end
end

-- Mouse use hides the focus ring.
function Pad.mouse_used() Pad.active = false end

function Pad.draw()
  if not Pad.active then return end
  local b = Pad.focused()
  if not b then return end
  love.graphics.setColor(1, .72, .2, 1)
  love.graphics.setLineWidth(4)
  love.graphics.rectangle("line", b.x - 5, b.y - 5, b.w + 10, b.h + 10, 8)
  love.graphics.setLineWidth(1)
end

return Pad
