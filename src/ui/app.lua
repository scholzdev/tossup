-- LÖVE callbacks: load, draw, update, input. Views live in src/ui/views/.
local Game = require("src.game")
local Profile = require("src.profile")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local Sound = require("src.ui.sound")
local Tutorial = require("src.ui.tutorial")
local Pad = require("src.ui.pad")
local views = {
  ENCOUNTER = require("src.ui.views.encounter"),
  SHOP = require("src.ui.views.shop"),
  CONTRACT = require("src.ui.views.contracts"),
}
local screens = {
  title = require("src.ui.views.title"),
  select = require("src.ui.views.menu"),
  collection = require("src.ui.views.collection"),
  sets = require("src.ui.views.sets"),
  options = require("src.ui.views.options"),
  help = require("src.ui.views.help"),
}
local draw_end = require("src.ui.views.finish")
local C, color, box, text = D.C, D.color, D.box, D.text

local app = {}

local function draw_game()
  local game = ui.game
  if game.phase == "ENCOUNTER" then views.ENCOUNTER()
  elseif game.phase == "SHOP" then views.SHOP()
  elseif game.phase == "CONTRACT" then views.CONTRACT()
  elseif game.phase == "GAME_OVER" and not game.over_seen then
    -- the last coin is gone and nothing can be exchanged: say so on the round screen before the run-over screen
    views.ENCOUNTER()
    if not ui.confirm then
      ui.confirm = {title = "GAME OVER", text = "NO COINS LEFT AND NO EXCHANGE POSSIBLE. THE RUN IS OVER.", single = true,
        ok = function() game.over_seen = true end}
    end
  else draw_end() end -- VICTORY and the run-over screen
  if ui.notice ~= "" then text(ui.notice, 300, 762, ui.f16, C.red) end
  if ui.debug_visible then
    box(944, 177, 300, 101, C.ink)
    text("DEBUG / F3", 955, 184, ui.f16, C.gold)
    text("RNG " .. game.rng_state, 955, 207, ui.f16)
    text("FLIPS " .. game.encounter.flips, 955, 231, ui.f16)
    text("LAST " .. (game.last_rng and string.format("%.5f", game.last_rng) or "-"), 955, 255, ui.f16)
  end
end

function app.load()
  love.graphics.setDefaultFilter("nearest", "nearest")
  local path = "assets/fonts/m6x11plus.ttf"
  ui.f16 = love.graphics.newFont(path, 16)
  ui.f20 = love.graphics.newFont(path, 20)
  ui.f32 = love.graphics.newFont(path, 32)
  ui.f48 = love.graphics.newFont(path, 48)
  for _, f in ipairs({ui.f16, ui.f20, ui.f32, ui.f48}) do f:setFilter("nearest", "nearest") end
  for id in pairs(ui.catalog) do
    local image = love.graphics.newImage("assets/coins/" .. id .. ".png", {mipmaps = true})
    image:setFilter("linear", "linear")
    image:setMipmapFilter("linear") -- smooth when a 512px coin is drawn small
    ui.coin_images[id] = image
  end
  local function load_image(path)
    local image = love.graphics.newImage(path, {mipmaps = true})
    image:setFilter("linear", "linear")
    image:setMipmapFilter("linear")
    return image
  end
  for id in pairs(ui.item_catalog) do ui.item_images[id] = load_image("assets/items/" .. id .. ".png") end
  for id in pairs(Game.relics()) do ui.relic_images[id] = load_image("assets/relics/" .. id .. ".png") end
  for _, name in ipairs({"next_round", "reroll", "gold", "energy", "coins_left", "open_shop", "exchange", "give_up",
    "flip", "discard", "next_coin", "start_level", "shop_title", "logo", "title_scene",
    "title_play", "title_sets", "title_collection", "title_options", "title_help",
    "title_play_de", "title_sets_de", "title_collection_de", "title_options_de", "title_help_de"}) do
    ui.ui_images[name] = load_image("assets/ui/" .. name .. ".png")
  end
  ui.ui_images.title_shop = ui.ui_images.shop_title
  ui.coin_images.back = love.graphics.newImage("assets/coins/back.png", {mipmaps = true})
  ui.coin_images.back:setMipmapFilter("linear")
  ui.coin_images.back:setFilter("linear", "linear")
  for _, id in ipairs(ui.character_order) do
    ui.character_images[id] = love.graphics.newImage("assets/characters/" .. id .. ".png")
    ui.character_images[id]:setFilter("nearest", "nearest")
  end
  Sound.load()
  A.load_profile()
  if Game.SANDBOX_MODE then A.start_sandbox() end
  -- game cursors: an arrow with a coin; it turns gold with the coin on edge over anything clickable
  ui.cursors = {
    arrow = love.mouse.newCursor(love.image.newImageData("assets/ui/cursor_arrow.png"), 2, 2),
    click = love.mouse.newCursor(love.image.newImageData("assets/ui/cursor_click.png"), 2, 2),
  }
  love.mouse.setCursor(ui.cursors.arrow)
  love.mouse.setPosition(0, 0)
end

function app.draw()
  ui.buttons = {}
  ui.hovered_coin = nil
  ui.hovered_text = nil
  ui.regions = {}
  ui.hover_anchor = nil
  local scale, ox, oy = ui.layout()
  love.graphics.clear(C.felt[1], C.felt[2], C.felt[3]) -- fills the bars around the 16:10 canvas
  love.graphics.push()
  love.graphics.translate(ox, oy)
  love.graphics.scale(scale)
  love.graphics.push()
  if ui.shake > 0 then
    love.graphics.translate(love.math.random(-6, 6) * ui.shake / .3, love.math.random(-6, 6) * ui.shake / .3)
  end
  color(C.felt)
  love.graphics.rectangle("fill", 0, 0, 1280, 800)
  if not ui.game or ui.game.paused then screens[ui.screen]() else draw_game() end
  Pad.inspect() -- controller / keyboard: show the details of the focused item
  D.coin_tooltip()
  D.text_tooltip()
  if ui.tutorial then Tutorial.draw() end
  D.confirm_dialog()
  Pad.draw() -- the controller focus ring, on top of everything
  love.graphics.pop()
  love.graphics.pop()
end

function app.update(dt)
  A.update(dt)
  Tutorial.update()
  Pad.update(dt, app)
  Sound.watch(ui)
  if ui.cursors then -- pick the cursor from what the last frame drew under the mouse
    local mx, my = ui.mouse()
    local over = false
    for _, b in ipairs(ui.buttons) do
      if not b.disabled and mx >= b.x and mx <= b.x + b.w and my >= b.y and my <= b.y + b.h then over = true break end
    end
    local want = over and ui.cursors.click or ui.cursors.arrow
    if want ~= ui.cursor_current then
      love.mouse.setCursor(want)
      ui.cursor_current = want
    end
  end
end

-- Press a button (mouse or controller): click sound, its action, and sliders start following the mouse.
function app.activate(b, x)
  if b.disabled then return end -- focusable for Inspect, but not pressable
  ui.notice = ""
  Sound.play("click")
  b.action()
  if x and b.drag then ui.dragging = b b.drag(x) end -- sliders follow the mouse until it is released
end

function app.discard_current() A.discard_current() end
function app.cycle_character(direction) A.cycle_character(direction) end
function app.change_collection_page(direction) A.change_collection_page(direction) end

-- Coin Sets: the next / previous character tab that is unlocked.
function app.cycle_sets_character(direction)
  local order = ui.character_order
  for i, id in ipairs(order) do
    if id == ui.sets_character then
      for step = 1, #order do
        local candidate = order[(i - 1 + direction * step) % #order + 1]
        if Profile.character_unlocked(ui.profile, candidate) then A.sets_pick_character(candidate) return end
      end
    end
  end
end

function app.mousepressed(x, y, mouse_button)
  if mouse_button ~= 1 then return end
  Pad.mouse_used()
  x, y = ui.to_canvas(x, y)
  for i = #ui.buttons, 1, -1 do
    local b = ui.buttons[i]
    if x >= b.x and x <= b.x + b.w and y >= b.y and y <= b.y + b.h then
      app.activate(b, x)
      return
    end
  end
end

function app.gamepadpressed(_, button) Pad.device = "controller" Pad.pressed(button, app) end

function app.mousemoved(x)
  Pad.mouse_used()
  if ui.dragging then ui.dragging.drag((ui.to_canvas(x, 0))) end
end

function app.mousereleased()
  local slider = ui.dragging
  ui.dragging = nil
  if slider and slider.release then slider.release() end
end

-- Keyboard: arrows move the focus ring, Enter presses the focused button, Esc = back, Space = flip / next coin, D = discard,
-- 1-3 = chips, O = open shop, Q / E = previous / next page, tab or character. They map onto the same actions as the controller.
local KEYS = {up = "dpup", down = "dpdown", left = "dpleft", right = "dpright", ["return"] = "a", kpenter = "a",
  q = "leftshoulder", e = "rightshoulder", i = "inspect"}

function app.keypressed(key)
  if key == "f5" and Game.SANDBOX_MODE then A.start_sandbox() return end
  local game = ui.game
  Pad.device = "keyboard"
  if key == "f3" then ui.debug_visible = not ui.debug_visible return end
  if ui.tutorial then
    if key == "escape" then Tutorial.finish() return end
    if (key == "space" or key == "return") and not Tutorial.interactive() then Tutorial.next() return end
  end
  if ui.confirm then -- a popup is open: Esc cancels it (or confirms a notice that has only OK); arrows + Enter pick a button
    if key == "escape" or key == "return" and ui.confirm.single then
      local c = ui.confirm
      ui.confirm = nil
      if c.single then c.ok() end
    elseif KEYS[key] then Pad.pressed(KEYS[key], app) end
    return
  end
  if key == "escape" then
    if game and not game.paused then A.open_menu()
    elseif ui.screen == "sets" then A.back_from_sets()
    elseif ui.screen ~= "title" then A.go("title")
    elseif game then game.paused = false end
    return
  end
  local in_run = game and not game.paused
  if in_run and not ui.tutorial and game.phase == "ENCOUNTER" and not game.mulligan and not ui.flip_animation then
    local slot = tonumber(key)
    if slot and slot >= 1 and slot <= 3 and ui.game.items[slot] and not ui.holding then A.use_item(slot) return end
    if key == "d" then Pad.pressed("y", app) return end
    if key == "o" and game.encounter.cleared and not game.pending then A.open_shop() return end
  end
  if not in_run and ui.screen == "select" then
    local choice = tonumber(key)
    if choice and ui.character_order[choice] then A.select_character(ui.character_order[choice]) return end
  end
  if key == "space" and in_run and game.phase == "ENCOUNTER" and not ui.flip_animation then
    A.next_or_flip()
    return
  end
  if KEYS[key] then Pad.pressed(KEYS[key], app) end
end

return app
