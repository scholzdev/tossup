-- Main menu, in the shop's full-screen style.
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local Tutorial = require("src.ui.tutorial")
local Version = require("src.version")
local C, color, centered, button, text = D.C, D.color, D.centered, D.button, D.text
local Game = require("src.game")

local function draw_title()
  color(C.white)
  love.graphics.draw(ui.ui_images.title_scene, 0, 0, 0, 1280 / ui.ui_images.title_scene:getWidth(), 800 / ui.ui_images.title_scene:getHeight())
  -- menu panel on the left, the scene stays visible on the right
  color(C.ink, .88)
  love.graphics.rectangle("fill", 70, 70, 400, 590, 10)
  D.outline(70, 70, 400, 590, C.gold)
  local logo = ui.ui_images.logo
  color(C.white)
  love.graphics.draw(logo, 120, 96, 0, 300 / logo:getWidth(), 300 / logo:getWidth())
  centered("BEAT THE QUOTA", 70, 190, 400, ui.f20, C.muted)

  local entries = {}
  if ui.game then
    entries[#entries + 1] = {"CONTINUE", C.blue, function() ui.game.paused = false end}
    entries[#entries + 1] = {"NEW RUN", C.gold, function()
      ui.confirm = {title = "NEW RUN", text = "YOUR SAVED RUN WILL BE REPLACED.", ok = A.play}
    end}
  elseif A.has_saved_run() then -- a run saved by a previous session
    entries[#entries + 1] = {"CONTINUE", C.blue, A.load_run}
    entries[#entries + 1] = {"NEW RUN", C.gold, function()
      ui.confirm = {title = "NEW RUN", text = "YOUR SAVED RUN WILL BE REPLACED.", ok = A.play}
    end}
  else
    entries[#entries + 1] = {"PLAY", C.blue, A.play}
  end
  entries[#entries + 1] = {"COIN SETS", C.gold, function() A.open_sets(ui.selected_character) end}
  entries[#entries + 1] = {"COLLECTION", C.green, function() A.go("collection") end}
  local small = {
    {"TUTORIAL", C.green, function()
      if ui.game then ui.confirm = {title = "TUTORIAL", text = "THIS LEVEL STARTS OVER WHEN YOU CONTINUE.", ok = Tutorial.start}
      else Tutorial.start() end
    end},
    {"HELP", C.green, function() ui.help_next = nil A.go("help") end},
    {"OPTIONS", C.panel_light, function() A.go("options") end},
  }

  local dev_string = Game.is_dev() and ".dev" or ""

  text("v" .. Version.number .. dev_string .. " (" .. Version.build .. ")", 96, 626, ui.f16, C.muted)

  local y = 240
  for _, entry in ipairs(entries) do
    button(entry[1], 100, y, 340, 52, entry[2], entry[3])
    y = y + 62
  end
  y = y + 10
  for i, entry in ipairs(small) do
    button(entry[1], 100 + (i - 1) * 114, y, 112, 44, entry[2], entry[3])
  end
  button("QUIT", 100, y + 62, 340, 44, C.red, A.quit)
end

return draw_title
