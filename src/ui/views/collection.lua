local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_hover = D.coin_hover

local COLUMNS, ROWS = 5, 3
local PER_PAGE = COLUMNS * ROWS
local CELL_W, ICON = 170, 88

-- rarity code (in the coin files) -> display name and tab colour
local TABS = {
  {key = "ALL", label = "All", fill = {.13, .27, .50}},
  {key = "N", label = "Common", fill = {.15, .19, .20}},
  {key = "R", label = "Uncommon", fill = {.07, .24, .20}},
  {key = "SR", label = "Rare", fill = {.28, .16, .04}},
  {key = "UR", label = "Epic", fill = {.20, .05, .14}},
}
local RANK = {N = 1, R = 2, SR = 3, UR = 4}
local SORTS = {{key = "rarity", label = "Rarity"}, {key = "name", label = "Name"}, {key = "order", label = "Default"}}

local function sort_label()
  for _, s in ipairs(SORTS) do if s.key == ui.collection_sort then return s.label end end
end

local function cycle_sort()
  for i, s in ipairs(SORTS) do
    if s.key == ui.collection_sort then
      ui.collection_sort = SORTS[i % #SORTS + 1].key
      ui.collection_page = 1
      return
    end
  end
end

local function visible_ids()
  local ids = {}
  for index, id in ipairs(ui.coin_order) do
    if ui.collection_filter == "ALL" or ui.catalog[id].rarity == ui.collection_filter then
      ids[#ids + 1] = {id = id, index = index}
    end
  end
  table.sort(ids, function(a, b)
    if ui.collection_sort == "rarity" then
      local ra, rb = RANK[ui.catalog[a.id].rarity] or 9, RANK[ui.catalog[b.id].rarity] or 9
      if ra ~= rb then return ra < rb end
    elseif ui.collection_sort == "name" then
      local na, nb = ui.catalog[a.id].name, ui.catalog[b.id].name
      if na ~= nb then return na < nb end
    end
    return a.index < b.index
  end)
  local list = {}
  for i, entry in ipairs(ids) do list[i] = entry.id end
  return list
end

-- A dark pill with light text (the shared button() draws dark text, which is unreadable here).
local function pill(label, x, y, w, h, fill, action, selected)
  local mx, my = ui.mouse()
  local hover = mx >= x and mx <= x + w and my >= y and my <= y + h
  box(x, y + (hover and -2 or 0), w, h, fill)
  if selected then outline(x, y, w, h, C.white) end
  centered(label, x, y + (h - ui.f20:getHeight()) / 2 + (hover and -2 or 0), w, ui.f20, C.white)
  ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = w, h = h, action = action}
end

-- a chevron drawn with lines so it needs no font glyph
local function chevron(cx, cy, size, direction)
  color(C.white)
  love.graphics.setLineWidth(4)
  love.graphics.line(cx - direction * size / 2, cy - size, cx + direction * size / 2, cy, cx - direction * size / 2, cy + size)
  love.graphics.setLineWidth(1)
end

local function draw_collection()
  D.frame(D.title("collection"), "BACK", function() A.go("title") end)

  -- sort + rarity filters
  text("SORT", 70, 168, ui.f16, C.muted)
  pill(sort_label(), 120, 156, 120, 40, TABS[1].fill, cycle_sort)
  color(C.white)
  love.graphics.rectangle("fill", 254, 152, 3, 48)
  for i, tab in ipairs(TABS) do
    local x = 274 + (i - 1) * 126
    pill(tab.label, x, 156 + (ui.collection_filter == tab.key and 3 or 0), 116, 40, tab.fill,
      function() A.set_filter(tab.key) end, ui.collection_filter == tab.key)
  end

  -- coin grid, five across
  local ids = visible_ids()
  local pages = math.max(1, math.ceil(#ids / PER_PAGE))
  ui.collection_page = math.min(ui.collection_page, pages)
  local first = (ui.collection_page - 1) * PER_PAGE
  local x0 = 640 - (COLUMNS * CELL_W) / 2
  for slot = 1, PER_PAGE do
    local id = ids[first + slot]
    if id then
      local x = x0 + ((slot - 1) % COLUMNS) * CELL_W
      local y = 224 + math.floor((slot - 1) / COLUMNS) * 154
      local collected = ui.profile.collected[id]
      local image = ui.coin_images[id]
      if collected then color(C.white) else love.graphics.setColor(0, 0, 0, .8) end
      love.graphics.draw(image, x + (CELL_W - ICON) / 2, y, 0, ICON / image:getWidth(), ICON / image:getHeight())
      box(x + 11, y + 100, CELL_W - 22, 32, C.card)
      outline(x + 11, y + 100, CELL_W - 22, 32, C.line)
      centered(collected and ui.catalog[id].name or "Uncollected", x + 11, y + 106, CELL_W - 22, ui.f20,
        collected and C.white or C.muted)
      if collected then coin_hover(id, x + 37, y, ICON, 132) end
    end
  end

  -- paging inside the frame
  local can_prev, can_next = ui.collection_page > 1, ui.collection_page < pages
  button("<", 70, 380, 50, 90, C.green, function() A.change_collection_page(-1) end, can_prev)
  button(">", 1160, 380, 50, 90, C.green, function() A.change_collection_page(1) end, can_next)
  centered(ui.collection_page .. "/" .. pages, 440, 700, 400, ui.f32, C.white)
  local owned = 0
  for _ in pairs(ui.profile.collected) do owned = owned + 1 end
  centered(D.L("COLLECTED %d / %d", owned, #ui.coin_order), 880, 710, 320, ui.f16, C.muted)
end

return draw_collection
