-- Drawing primitives shared by every view.
local ui = require("src.ui.state")
local C = require("src.ui.theme")
local Lang = require("src.lang")
local L = Lang.t
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
  str = L(str)
  love.graphics.setFont(face or ui.f16)
  color(tint or C.face)
  love.graphics.print(str, math.floor(x), math.floor(y))
end
local function centered(str, x, y, w, face, tint)
  face = face or ui.f20
  str = L(str)
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
  if #effects_list == 0 then return L("Nothing") end
  local parts = {}
  for _, e in ipairs(effects_list) do
    if e.type == "next_mult" then parts[#parts + 1] = L("NEXT %d x%d", e.coins, e.amount) goto continue end
    if e.type == "next_odds" then parts[#parts + 1] = L("NEXT %d +%d%%", e.coins, math.floor(e.amount * 100 + .5)) goto continue end
    if e.type == "amplify" then parts[#parts + 1] = L("AMPLIFY") goto continue end
    if e.type == "combo_bonus" then parts[#parts + 1] = L("COMBO +%d", e.amount) goto continue end
    if e.type == "combo_shield" then parts[#parts + 1] = L("COMBO SHIELD") goto continue end
    if e.type == "next_swap" then parts[#parts + 1] = L("NEXT: SWAP") goto continue end
    if e.type == "next_heads" then parts[#parts + 1] = L("NEXT: HEADS") goto continue end
    local amount = e.type == "probability" and math.floor(e.amount * 100 + .5) or e.amount
    local labels = {score = L("PTS"), gold = L("GOLD"), energy = L("NRG"), penalty = L("QUOTA"),
      extra_draw = L("REPLAY"), probability = L("% HEADS")}
    local prefix = "+"
    parts[#parts + 1] = prefix .. amount .. " " .. (labels[e.type] or e.type)
    ::continue::
  end
  return table.concat(parts, ", ")
end

local function effect_description(effects_list)
  if #effects_list == 0 then return L("No effect") end
  local parts = {}
  for _, effect in ipairs(effects_list) do
    local amount = effect.amount
    if effect.type == "score" then parts[#parts + 1] = L("Score %d points", amount)
    elseif effect.type == "gold" then parts[#parts + 1] = L("Gain %d gold", amount)
    elseif effect.type == "energy" then parts[#parts + 1] = L("Gain %d energy", amount)
    elseif effect.type == "penalty" then parts[#parts + 1] = L("Quota +%d", amount)
    elseif effect.type == "extra_draw" then parts[#parts + 1] = L("Goes back into the pile")
    elseif effect.type == "probability" then
      parts[#parts + 1] = L("Gain %d%% Heads this level", math.floor(amount * 100 + .5))
    elseif effect.type == "next_mult" then
      parts[#parts + 1] = L("Next %d coins pay x%d", effect.coins, amount)
    elseif effect.type == "next_odds" then
      local pct = math.floor(amount * 100 + .5)
      parts[#parts + 1] = effect.coins == 1 and L("Next coin: +%d%% Heads", pct) or L("Next %d coins: +%d%% Heads", effect.coins, pct)
    elseif effect.type == "amplify" then parts[#parts + 1] = L("Buffs last 1 coin longer and get stronger")
    elseif effect.type == "combo_bonus" then parts[#parts + 1] = L("Combo grows %d extra step", amount)
    elseif effect.type == "combo_shield" then parts[#parts + 1] = L("The next combo break is prevented")
    elseif effect.type == "next_swap" then parts[#parts + 1] = L("Next coin uses its other side")
    elseif effect.type == "next_heads" then parts[#parts + 1] = L("Next coin lands Heads")
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

-- A button with an icon on the left and its label centred in the rest.
local function icon_button(label, icon, x, y, w, h, tint, action, enabled)
  local active = enabled ~= false
  local mx, my = ui.mouse()
  local hover = active and mx >= x and mx <= x + w and my >= y and my <= y + h
  local top = y + (hover and -3 or 0)
  box(x, top, w, h, active and tint or C.panel_light)
  outline(x, top, w, h, active and C.face or C.slot)
  local size = h - 12
  if active then color(C.white) else love.graphics.setColor(1, 1, 1, .45) end
  love.graphics.draw(icon, x + 8, top + 6, 0, size / icon:getWidth(), size / icon:getHeight())
  centered(label, x + size + 8, top + (h - ui.f20:getHeight()) / 2, w - size - 8, ui.f20, active and C.ink or C.muted)
  if active then ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = w, h = h, action = action} end
end

-- The shared full-screen look (the shop's): felt backdrop, a teal screen with a gold border, a pixel
-- title image at the top left and, when given, a button at the top right.
local function frame(title_image, back_label, back_action)
  box(0, 0, 1280, 800, C.felt_dark)
  box(36, 36, 1208, 728, C.screen)
  outline(36, 36, 1208, 728, C.gold)
  if title_image then
    local scale = 90 / title_image:getHeight()
    color(C.white)
    love.graphics.draw(title_image, 70, 46, 0, scale, scale)
  end
  if back_action then button(back_label, 1120, 56, 100, 34, C.panel_light, back_action) end
end

-- The pixel title image for a screen, in the current language when there is one.
local function title(name)
  return (Lang.current ~= "en" and ui.ui_images["title_" .. name .. "_" .. Lang.current]) or ui.ui_images["title_" .. name]
end

-- A modal popup (ui.confirm = {title, text, ok = function}): dims the screen, shows the text with OK and Cancel, and
-- replaces every other clickable while it is open. Draw it last.
local function confirm_dialog()
  local c = ui.confirm
  if not c then return end
  ui.buttons = {} -- nothing behind the popup can be clicked
  color(C.ink, .72)
  love.graphics.rectangle("fill", 0, 0, 1280, 800)
  box(380, 270, 520, 260, C.panel_dk)
  outline(380, 270, 520, 260, C.red)
  centered(c.title, 380, 292, 520, ui.f32, C.red)
  love.graphics.setFont(ui.f20)
  color(C.face)
  love.graphics.printf(L(c.text), 410, 352, 460, "center")
  if c.single then -- a notice: just OK
    button("OK", 540, 454, 200, 52, C.red, function() ui.confirm = nil c.ok() end)
  else
    button("OK", 420, 454, 200, 52, C.red, function() ui.confirm = nil c.ok() end)
    button("CANCEL", 660, 454, 200, 52, C.panel_light, function() ui.confirm = nil end)
  end
end

-- A small padlock centred on (cx, cy): locked coins and deck slots.
local function padlock(cx, cy)
  color(C.face)
  love.graphics.setLineWidth(3)
  love.graphics.arc("line", "open", cx, cy - 2, 6, math.pi, 2 * math.pi)
  love.graphics.setLineWidth(1)
  love.graphics.rectangle("fill", cx - 9, cy - 2, 18, 14, 2)
  color(C.ink)
  love.graphics.circle("fill", cx, cy + 5, 2)
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
  love.graphics.printf(L(tip.body), x + 14, y + 44, w - 28)
end

local function draw_coin_tooltip()
  if not ui.hovered_coin then return end
  local coin = catalog[ui.hovered_coin.id]
  local mx, my = ui.mouse()
  local w, h = 390, ui.hovered_coin.locked and 204 or 172
  local _, lines = ui.f16:getWrap(coin.description, w - 92) -- long descriptions wrap and push the rest down
  local extra = math.max(0, #lines - 1) * 18
  h = h + extra
  local x = math.min(mx + 18, 1280 - w - 12)
  local y = my + 18
  if y + h > 788 then y = my - h - 18 end
  y = math.max(12, y)
  box(x, y, w, h, C.ink)
  outline(x, y, w, h, C.gold)
  coin_image(ui.hovered_coin.id, x + 9, y + 9, 62)
  text(coin.name:upper(), x + 78, y + 12, ui.f20, C.face)
  love.graphics.setFont(ui.f16)
  color(C.muted)
  love.graphics.printf(coin.description, x + 78, y + 40, w - 92)
  local heads = math.floor(ui.hovered_coin.probability * 100 + .5)
  text(L("HEADS %d%%  /  TAILS %d%%", heads, 100 - heads) ..
    ((coin.energy_cost or 0) > 0 and L("  -  COSTS %d ENERGY", coin.energy_cost) or ""), x + 14, y + 78 + extra, ui.f16, C.gold)
  text(L("HEADS") .. "  " .. effect_description(coin.heads), x + 14, y + 108 + extra, ui.f16, C.blue)
  text(L("TAILS") .. "  " .. effect_description(coin.tails), x + 14, y + 137 + extra, ui.f16, C.red)
  if ui.hovered_coin.locked then
    text("LOCKED  -  BUY IT IN THE SHOP TO UNLOCK", x + 14, y + 172 + extra, ui.f16, C.orange)
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
  button = button, coin_image = coin_image, image_at = image_at, icon_button = icon_button, frame = frame, title = title, padlock = padlock, confirm_dialog = confirm_dialog, coin_name = coin_name, coin_face = coin_face, coin_hover = coin_hover,
  effects = effects, effect_description = effect_description, coin_tooltip = draw_coin_tooltip, L = L, text_hover = text_hover, text_tooltip = draw_text_tooltip}
