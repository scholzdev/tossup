local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, box, outline, centered, button = D.C, D.box, D.outline, D.centered, D.button

local function draw_title()
  box(344, 67, 592, 670, C.ink)
  outline(344, 67, 592, 670, C.gold)
  centered("TOSSUP", 344, 120, 592, ui.f48, C.gold)
  centered("BEAT THE QUOTA", 344, 182, 592, ui.f16, C.muted)

  local entries = {}
  if ui.game then
    entries[#entries + 1] = {"CONTINUE", C.blue, function() ui.game.paused = false end}
    entries[#entries + 1] = {"NEW RUN", C.gold, function() A.go("select") end}
  else
    entries[#entries + 1] = {"PLAY", C.blue, function() A.go("select") end}
  end
  entries[#entries + 1] = {"COLLECTION", C.green, function() A.go("collection") end}
  entries[#entries + 1] = {"OPTIONS", C.panel_light, function() A.go("options") end}
  entries[#entries + 1] = {"QUIT", C.red, function() love.event.quit() end}
  for i, entry in ipairs(entries) do
    button(entry[1], 470, 240 + (i - 1) * 84, 340, 60, entry[2], entry[3])
  end
end

return draw_title
