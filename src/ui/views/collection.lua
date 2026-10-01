local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_hover = D.coin_hover

local COLUMNS, ROWS = 5, 3
local PER_PAGE = COLUMNS * ROWS
local CELL_W, ICON = 170, 96

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
  color(C.felt_dark) -- oversized so it also covers the bars of a wide window
  love.graphics.rectangle("fill", -2000, -2000, 5280, 4800)

  -- header plate and back arrow
  box(360, -10, 560, 90, C.gold)
  centered("Coin Collection", 360, 18, 560, ui.f48, C.white)
  local mx, my = ui.mouse()
  local back_hover = mx >= 20 and mx <= 130 and my >= 22 and my <= 78
  box(20, 22 + (back_hover and -2 or 0), 110, 56, C.gold)
  color(C.white)
  love.graphics.polygon("fill", 36, 50, 62, 32, 62, 43, 112, 43, 112, 57, 62, 57, 62, 68)
  ui.buttons[#ui.buttons + 1] = {x = 20, y = 22, w = 110, h = 56, action = function() A.go("title") end}

  -- sort + rarity filters
  text("Sort by", 130, 126, ui.f20, C.muted)
  pill(sort_label(), 205, 108, 120, 50, TABS[1].fill, cycle_sort)
  color(C.white)
  love.graphics.rectangle("fill", 342, 100, 3, 66)
  for i, tab in ipairs(TABS) do
    local x = 365 + (i - 1) * 150
    pill(tab.label, x, 108 + (ui.collection_filter == tab.key and 5 or 0), 138, 50, tab.fill,
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
      local y = 195 + math.floor((slot - 1) / COLUMNS) * 160
      local collected = ui.profile.collected[id]
      local image = ui.coin_images[id]
      if collected then
        color(C.white)
      else
        love.graphics.setColor(0, 0, 0, .85) -- silhouette of a coin you have not owned yet
      end
      love.graphics.draw(image, x + (CELL_W - ICON) / 2, y, 0, ICON / image:getWidth(), ICON / image:getHeight())
      box(x + 11, y + 102, CELL_W - 22, 34, {.42, .50, .62})
      centered(collected and ui.catalog[id].name or "Uncollected", x + 11, y + 109, CELL_W - 22, ui.f20,
        collected and C.white or C.panel_light)
      if collected then coin_hover(id, x + 37, y, ICON, 136) end
    end
  end

  -- paging: a slim arrow on the left, a big block on the right, "1/3" at the bottom
  local can_prev, can_next = ui.collection_page > 1, ui.collection_page < pages
  color(can_prev and {.20, .26, .30} or {.12, .17, .19})
  love.graphics.rectangle("fill", 10, 335, 54, 70, 4)
  chevron(40, 370, 12, -1)
  ui.buttons[#ui.buttons + 1] = can_prev and {x = 10, y = 335, w = 54, h = 70, action = function() A.change_collection_page(-1) end} or nil
  box(1170, 300, 110, 190, can_next and {.13, .27, .50} or {.12, .17, .25})
  chevron(1225, 395, 18, 1)
  ui.buttons[#ui.buttons + 1] = can_next and {x = 1170, y = 300, w = 110, h = 190, action = function() A.change_collection_page(1) end} or nil
  centered(ui.collection_page .. "/" .. pages, 440, 700, 400, ui.f32, C.white)
  local owned = 0
  for _ in pairs(ui.profile.collected) do owned = owned + 1 end
  centered("Collected " .. owned .. " / " .. #ui.coin_order, 940, 750, 320, ui.f16, C.muted)
end

return draw_collection
