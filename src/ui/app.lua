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
local screens = {
  title = require("src.ui.views.title"),
  select = require("src.ui.views.menu"),
  collection = require("src.ui.views.collection"),
  sets = require("src.ui.views.sets"),
  options = require("src.ui.views.options"),
}
local draw_end = require("src.ui.views.finish")
local C, color, box, text = D.C, D.color, D.box, D.text

local app = {}

local function draw_game()
  local game = ui.game
  if game.phase == "ENCOUNTER" then
    views.ENCOUNTER()
  else
    if game.phase == "SHOP" then
      views.SHOP() -- the shop is a full-screen view
    else
      Common.sidebar()
      box(280, 16, 984, 768, C.felt_dark)
      D.outline(280, 16, 984, 768, C.panel_light)
      draw_end()
    end
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
    "flip", "discard", "next_coin", "start_level", "shop_title"}) do
    ui.ui_images[name] = load_image("assets/ui/" .. name .. ".png")
  end
  ui.coin_images.back = love.graphics.newImage("assets/coins/back.png", {mipmaps = true})
  ui.coin_images.back:setMipmapFilter("linear")
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
  ui.hovered_text = nil
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
  D.coin_tooltip()
  D.text_tooltip()
  love.graphics.pop()
  love.graphics.pop()
end

app.update = A.update

function app.mousepressed(x, y, mouse_button)
  if mouse_button ~= 1 then return end
  x, y = ui.to_canvas(x, y)
  for i = #ui.buttons, 1, -1 do
    local b = ui.buttons[i]
    if x >= b.x and x <= b.x + b.w and y >= b.y and y <= b.y + b.h then
      ui.notice = ""
      b.action()
      return
    end
  end
end

function app.keypressed(key)
  local game = ui.game
  if key == "f3" then ui.debug_visible = not ui.debug_visible return end
  if key == "escape" then
    if game and not game.paused then A.open_menu()
    elseif ui.screen ~= "title" then A.go("title")
    elseif game then game.paused = false end
    return
  end
  if not game or game.paused then
    if ui.screen == "select" then
      local choice = tonumber(key)
      if choice and ui.character_order[choice] then A.select_character(ui.character_order[choice]) end
      if key == "left" then A.cycle_character(-1) end
      if key == "right" then A.cycle_character(1) end
      if key == "return" then A.start() end
    elseif ui.screen == "collection" then
      if key == "left" then A.change_collection_page(-1) end
      if key == "right" then A.change_collection_page(1) end
    end
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
