local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_hover = D.coin_hover

local COLUMNS, ROWS = 5, 3
local PER_PAGE = COLUMNS * ROWS
local TABS = {
  {"ALL", C.blue}, {"N", {.70, .76, .78}}, {"R", C.green}, {"SR", C.orange}, {"UR", {.55, .12, .12}},
}

local function visible_ids()
  local ids = {}
  for _, id in ipairs(ui.coin_order) do
    if ui.collection_filter == "ALL" or ui.catalog[id].rarity == ui.collection_filter then
      ids[#ids + 1] = id
    end
  end
  return ids
end

local function draw_collection()
  color(C.felt_dark) -- oversized so it also covers the bars of a wide window
  love.graphics.rectangle("fill", -2000, -2000, 5280, 4800)
  box(400, -10, 480, 80, C.gold)
  centered("COIN COLLECTION", 400, 22, 480, ui.f32, C.ink)
  button("X", 24, 24, 64, 64, C.gold, function() A.go("title") end)

  for i, tab in ipairs(TABS) do
    local x = 375 + (i - 1) * 110
    local selected = ui.collection_filter == tab[1]
    button(tab[1], x, 100 + (selected and 4 or 0), 90, 50, tab[2], function() A.set_filter(tab[1]) end)
    if selected then outline(x, 104, 90, 50, C.white) end
  end

  local ids = visible_ids()
  local pages = math.max(1, math.ceil(#ids / PER_PAGE))
  ui.collection_page = math.min(ui.collection_page, pages)
  local first = (ui.collection_page - 1) * PER_PAGE
  for slot = 1, PER_PAGE do
    local id = ids[first + slot]
    if id then
      local x = 265 + ((slot - 1) % COLUMNS) * 150
      local y = 180 + math.floor((slot - 1) / COLUMNS) * 170
      local collected = ui.profile.collected[id]
      if collected then
        D.coin_image(id, x + 20, y, 90)
      else
        love.graphics.setColor(0, 0, 0, .85) -- silhouette of a coin you have not owned yet
        local image = ui.coin_images[id]
        love.graphics.draw(image, x + 20, y, 0, 90 / image:getWidth(), 90 / image:getHeight())
      end
      box(x, y + 98, 130, 32, C.blue)
      centered(collected and ui.catalog[id].name:upper() or "UNCOLLECTED", x, y + 104, 130, ui.f16,
        collected and C.face or C.muted)
      if collected then coin_hover(id, x + 20, y, 90, 130) end
    end
  end

  button("<", 130, 330, 90, 170, C.blue, function() A.change_collection_page(-1) end, ui.collection_page > 1)
  button(">", 1060, 330, 90, 170, C.blue, function() A.change_collection_page(1) end, ui.collection_page < pages)
  centered(ui.collection_page .. " / " .. pages, 440, 720, 400, ui.f32, C.face)
  local owned = 0
  for _ in pairs(ui.profile.collected) do owned = owned + 1 end
  centered("COLLECTED " .. owned .. " / " .. #ui.coin_order, 880, 730, 340, ui.f16, C.muted)
end

return draw_collection
