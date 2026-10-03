local Game = require("src.game")
local ui = require("src.ui.state")
local D = require("src.ui.draw")

local function draw_run_modifiers(x, y, size)
  local game = ui.game
  local entries = {}
  if game.run_encounter_id then
    entries[#entries + 1] = {id = game.run_encounter_id, kind = "encounter"}
  end
  for _, id in ipairs(game.augments or {}) do
    entries[#entries + 1] = {id = id, kind = "augment"}
  end

  for i, entry in ipairs(entries) do
    local def = entry.kind == "encounter" and Game.encounter_def(entry.id) or Game.augment_def(entry.id)
    local images = entry.kind == "encounter" and ui.encounter_images or ui.augment_images
    local image = images[entry.id]
    if def and image then
      local at_x = x + (i - 1) * (size + 5)
      D.image_at(image, at_x, y, size)
      D.text_hover(D.L(def.name), D.L(def.description), at_x, y, size, size)
    end
  end
end

return draw_run_modifiers
