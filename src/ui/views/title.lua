-- Main menu, in the shop's full-screen style.
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local Tutorial = require("src.ui.tutorial")
local Version = require("src.version")
local C, color, centered, button, text = D.C, D.color, D.centered, D.button, D.text

local function draw_title()
  D.frame(nil)
  local logo = ui.ui_images.logo
  color(C.white)
  love.graphics.draw(logo, 640 - 230, 70, 0, 460 / logo:getWidth(), 460 / logo:getWidth())
  centered("BEAT THE QUOTA", 0, 204, 1280, ui.f20, C.muted)

  local entries = {}
  if ui.game then
    entries[#entries + 1] = {"CONTINUE", C.blue, function() ui.game.paused = false end}
    entries[#entries + 1] = {"NEW RUN", C.gold, A.play}
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
  entries[#entries + 1] = {"TUTORIAL", C.green, function()
    if ui.game then ui.confirm = {title = "TUTORIAL", text = "THIS LEVEL STARTS OVER WHEN YOU CONTINUE.", ok = Tutorial.start}
    else Tutorial.start() end
  end}
  entries[#entries + 1] = {"HOW TO PLAY", C.green, function() ui.help_next = nil A.go("help") end}
  entries[#entries + 1] = {"OPTIONS", C.panel_light, function() A.go("options") end}
  entries[#entries + 1] = {"QUIT", C.red, A.quit}
  text("v" .. Version.number .. " (" .. Version.build .. ")", 56, 736, ui.f16, C.muted)
  for i, entry in ipairs(entries) do
    button(entry[1], 470, 240 + (i - 1) * 58, 340, 48, entry[2], entry[3])
  end
end

return draw_title
