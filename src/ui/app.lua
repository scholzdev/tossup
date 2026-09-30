-- LÖVE callbacks: load, draw, update, input. Views live in src/ui/views/.
local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local Common = require("src.ui.views.common")
local views = {
  ENCOUNTER = require("src.ui.views.encounter"),
  SHOP = require("src.ui.views.shop"),
}
local draw_menu = require("src.ui.views.menu")
local draw_end = require("src.ui.views.finish")
local C, color, box, text = D.C, D.color, D.box, D.text

local app = {}

local function draw_game()
  local game = ui.game
  if game.phase == "ENCOUNTER" then
    views.ENCOUNTER()
  else
    Common.sidebar()
    box(280, 16, 984, 768, C.felt_dark)
    D.outline(280, 16, 984, 768, C.panel_light)
    local view = views[game.phase]
    if view then view() else draw_end() end
  end
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
    local image = love.graphics.newImage("assets/coins/" .. id .. ".png")
    image:setFilter("linear", "linear")
    ui.coin_images[id] = image
  end
  ui.coin_images.back = love.graphics.newImage("assets/coins/back.png")
  ui.coin_images.back:setFilter("linear", "linear")
  for _, id in ipairs(ui.character_order) do
    ui.character_images[id] = love.graphics.newImage("assets/characters/" .. id .. ".png")
    ui.character_images[id]:setFilter("nearest", "nearest")
  end
  A.load_profile()
  love.mouse.setPosition(0, 0)
end

function app.draw()
  ui.buttons = {}
  ui.hovered_coin = nil
  love.graphics.push()
  if ui.shake > 0 then
    love.graphics.translate(love.math.random(-6, 6) * ui.shake / .3, love.math.random(-6, 6) * ui.shake / .3)
  end
  color(C.felt)
  love.graphics.rectangle("fill", 0, 0, 1280, 800)
  if not ui.game or ui.game.paused then draw_menu() else draw_game() end
  D.coin_tooltip()
  love.graphics.pop()
end

app.update = A.update

function app.mousepressed(x, y, mouse_button)
  if mouse_button ~= 1 then return end
  for i = #ui.buttons, 1, -1 do
    local b = ui.buttons[i]
    if x >= b.x and x <= b.x + b.w and y >= b.y and y <= b.y + b.h then
      ui.notice = ""
      b.action()
      return
    end
  end
end

function app.wheelmoved(_, y)
  if not ui.game or ui.game.paused then
    if y < 0 then A.change_coin_page(1) elseif y > 0 then A.change_coin_page(-1) end
  end
end

function app.keypressed(key)
  local game = ui.game
  if key == "f3" then ui.debug_visible = not ui.debug_visible return end
  if key == "escape" then if game then game.paused = not game.paused end return end
  if not game or game.paused then
    local choice = tonumber(key)
    if choice and ui.character_order[choice] then A.select_character(ui.character_order[choice]) end
    if key == "pageup" or key == "[" then A.change_coin_page(-1) end
    if key == "pagedown" or key == "]" then A.change_coin_page(1) end
    if key == "return" then if game then game.paused = false else A.start() end end
    return
  end
  if game.phase == "ENCOUNTER" and ui.flip_animation then return end
  if key == "space" and game.phase == "ENCOUNTER" then
    A.next_or_flip()
  elseif key == "return" and game.phase == "SHOP" then
    Game.leave_shop(game)
  end
end

return app
