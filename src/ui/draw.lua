-- Drawing primitives shared by every view.
local ui = require("src.ui.state")
local C = require("src.ui.theme")
local catalog = ui.catalog

local function color(c, alpha)
  love.graphics.setColor(c[1], c[2], c[3], alpha or 1)
end
local function box(x, y, w, h, fill, radius)
  color(C.black, .34)
  love.graphics.rectangle("fill", x, y + 5, w, h, radius or 6)
  color(fill)
  love.graphics.rectangle("fill", x, y, w, h, radius or 6)
end
local function outline(x, y, w, h, tint, radius)
  color(tint)
  love.graphics.setLineWidth(2)
  love.graphics.rectangle("line", x + 1, y + 1, w - 2, h - 2, radius or 6)
  love.graphics.setLineWidth(1)
end
local function text(str, x, y, face, tint)
  love.graphics.setFont(face or ui.f16)
  color(tint or C.face)
  love.graphics.print(str, math.floor(x), math.floor(y))
end
local function centered(str, x, y, w, face, tint)
  face = face or ui.f20
  text(str, x + math.floor((w - face:getWidth(str)) / 2), y, face, tint)
end
local function button(str, x, y, w, h, tint, action, enabled)
  local active = enabled ~= false
  local mx, my = ui.mouse()
  local hover = active and mx >= x and mx <= x + w and my >= y and my <= y + h
  local fill = active and tint or C.panel_light
  box(x, y + (hover and -3 or 0), w, h, fill)
  outline(x, y + (hover and -3 or 0), w, h, active and C.face or C.slot)
  centered(str, x, y + (h - ui.f20:getHeight()) / 2 - (hover and 3 or 0), w, ui.f20,
    active and C.ink or C.muted)
  if active then
    ui.buttons[#ui.buttons + 1] = {x = x, y = y - (hover and 3 or 0), w = w, h = h, action = action}
  end
end

local function effects(effects_list)
  if #effects_list == 0 then return "Nothing" end
  local parts = {}
  for _, e in ipairs(effects_list) do
    local amount = e.type == "probability" and math.floor(e.amount * 100 + .5) or e.amount
    local labels = {score = "PTS", gold = "GOLD", energy = "NRG", penalty = "QUOTA",
      extra_draw = "REPLAY", probability = "% HEADS"}
    local prefix = "+"
    parts[#parts + 1] = prefix .. amount .. " " .. (labels[e.type] or e.type)
  end
  return table.concat(parts, ", ")
end

local function effect_description(effects_list)
  if #effects_list == 0 then return "No effect" end
  local parts = {}
  for _, effect in ipairs(effects_list) do
    local amount = effect.amount
    if effect.type == "score" then parts[#parts + 1] = "Score " .. amount .. " points"
    elseif effect.type == "gold" then parts[#parts + 1] = "Gain " .. amount .. " gold"
    elseif effect.type == "energy" then parts[#parts + 1] = "Gain " .. amount .. " energy"
    elseif effect.type == "penalty" then parts[#parts + 1] = "Quota +" .. amount
    elseif effect.type == "extra_draw" then parts[#parts + 1] = "Goes back into the pile"
    elseif effect.type == "probability" then
      parts[#parts + 1] = "Gain " .. math.floor(amount * 100 + .5) .. "% Heads this level"
    end
  end
  return table.concat(parts, "; ")
end

local function coin_hover(id, x, y, w, h, probability, locked)
  local mx, my = ui.mouse()
  if mx >= x and mx <= x + w and my >= y and my <= y + h then
    ui.hovered_coin = {id = id, probability = probability or catalog[id].probability,
      locked = locked}
  end
end

local function coin_image(id, x, y, size)
  color(C.white)
  local image = ui.coin_images[id]
  love.graphics.draw(image, x, y, 0, size / image:getWidth(), size / image:getHeight())
end

-- Draw any loaded image scaled to a square size.
local function image_at(image, x, y, size)
  color(C.white)
  love.graphics.draw(image, x, y, 0, size / image:getWidth(), size / image:getHeight())
end

local function coin_name(id, x, y, w)
  local name = catalog[id].name:upper()
  if w then centered(name, x, y, w, ui.f20, C.face) else text(name, x, y, ui.f20, C.face) end
end

-- Plain title + text tooltip (items, relics). Register while drawing; drawn once per frame on top.
local function text_hover(title, body, x, y, w, h)
  local mx, my = ui.mouse()
  if mx >= x and mx <= x + w and my >= y and my <= y + h then ui.hovered_text = {title = title, body = body} end
end

local function draw_text_tooltip()
  local tip = ui.hovered_text
  if not tip then return end
  local mx, my = ui.mouse()
  local w, h = 330, 92
  local x = math.min(mx + 18, 1280 - w - 12)
  local y = math.max(12, math.min(my + 18, 788 - h))
  box(x, y, w, h, C.ink)
  outline(x, y, w, h, C.gold)
  text(tip.title:upper(), x + 14, y + 12, ui.f20, C.face)
  love.graphics.setFont(ui.f16)
  color(C.muted)
  love.graphics.printf(tip.body, x + 14, y + 44, w - 28)
end

local function draw_coin_tooltip()
  if not ui.hovered_coin then return end
  local coin = catalog[ui.hovered_coin.id]
  local mx, my = ui.mouse()
  local w, h = 390, ui.hovered_coin.locked and 204 or 172
  local x = math.min(mx + 18, 1280 - w - 12)
  local y = my + 18
  if y + h > 788 then y = my - h - 18 end
  y = math.max(12, y)
  box(x, y, w, h, C.ink)
  outline(x, y, w, h, C.gold)
  coin_image(ui.hovered_coin.id, x + 9, y + 9, 62)
  text(coin.name:upper(), x + 78, y + 12, ui.f20, C.face)
  text(coin.description, x + 78, y + 40, ui.f16, C.muted)
  local heads = math.floor(ui.hovered_coin.probability * 100 + .5)
  text("HEADS " .. heads .. "%  /  TAILS " .. (100 - heads) .. "%" ..
    ((coin.energy_cost or 0) > 0 and ("  -  COSTS " .. coin.energy_cost .. " ENERGY") or ""), x + 14, y + 78, ui.f16, C.gold)
  text("HEADS  " .. effect_description(coin.heads), x + 14, y + 108, ui.f16, C.blue)
  text("TAILS  " .. effect_description(coin.tails), x + 14, y + 137, ui.f16, C.red)
  if ui.hovered_coin.locked then
    text("LOCKED  -  BUY IT IN THE SHOP TO UNLOCK", x + 14, y + 172, ui.f16, C.orange)
  end
end

local function coin_face(cx, cy, radius, outcome, selected, id)
  local size = radius * 2.6
  if selected then
    color(C.orange, .35)
    love.graphics.circle("fill", cx, cy, radius + 4)
  end
  coin_image(id or "copper", cx - size / 2, cy - size / 2, size)
  if outcome then
    local tint = outcome == "Heads" and C.blue or C.red
    local bx, by = cx + radius * .78, cy + radius * .65
    color(C.ink)
    love.graphics.circle("fill", bx, by, 15)
    color(tint)
    love.graphics.circle("fill", bx, by, 12)
    centered(outcome:sub(1, 1), bx - 13, by - ui.f20:getHeight() / 2, 26, ui.f20, C.ink)
  end
end

return {C = C, color = color, box = box, outline = outline, text = text, centered = centered,
  button = button, coin_image = coin_image, image_at = image_at, coin_name = coin_name, coin_face = coin_face, coin_hover = coin_hover,
  effects = effects, effect_description = effect_description, coin_tooltip = draw_coin_tooltip, text_hover = text_hover, text_tooltip = draw_text_tooltip}
