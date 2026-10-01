local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, centered, button = D.C, D.color, D.box, D.outline, D.centered, D.button

local function draw_title()
  box(344, 67, 592, 670, C.screen)
  outline(344, 67, 592, 670, C.gold)
  color(C.white)
  local logo = ui.ui_images.logo
  love.graphics.draw(logo, 640 - 180, 86, 0, 360 / logo:getWidth(), 360 / logo:getWidth())
  centered("BEAT THE QUOTA", 344, 208, 592, ui.f16, C.muted)

  local entries = {}
  if ui.game then
    entries[#entries + 1] = {"CONTINUE", C.blue, function() ui.game.paused = false end}
    entries[#entries + 1] = {"NEW RUN", C.gold, function() A.go("select") end}
  else
    entries[#entries + 1] = {"PLAY", C.blue, function() A.go("select") end}
  end
  entries[#entries + 1] = {"COIN SETS", C.gold, function() A.open_sets(ui.selected_character) end}
  entries[#entries + 1] = {"COLLECTION", C.green, function() A.go("collection") end}
  entries[#entries + 1] = {"OPTIONS", C.panel_light, function() A.go("options") end}
  entries[#entries + 1] = {"QUIT", C.red, function() love.event.quit() end}
  for i, entry in ipairs(entries) do
    button(entry[1], 470, 235 + (i - 1) * 78, 340, 58, entry[2], entry[3])
  end
end

return draw_title
