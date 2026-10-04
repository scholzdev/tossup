-- Drawing primitives shared by every view.
local ui = require("src.ui.state")
local Game = require("src.game")
local C = require("src.ui.theme")
local Lang = require("src.lang")
local L = Lang.t
local catalog = ui.catalog
local RARITY_NAME = {N = "Common", R = "Uncommon", SR = "Rare", UR = "Epic"}

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
local function button(str, x, y, w, h, tint, action, enabled, hotkey)
  local active = enabled ~= false
  local mx, my = ui.mouse()
  local hover = active and mx >= x and mx <= x + w and my >= y and my <= y + h
  local fill = active and tint or C.panel_light
  box(x, y + (hover and -3 or 0), w, h, fill)
  outline(x, y + (hover and -3 or 0), w, h, active and C.face or C.slot)
  centered(str, x, y + (h - ui.f20:getHeight()) / 2 - (hover and 3 or 0), w, ui.f20,
    active and C.ink or C.muted)
  -- a disabled button is still in the list (the controller can focus it, e.g. to inspect a coin you cannot buy), but pressing it does nothing
  ui.buttons[#ui.buttons + 1] = {x = x, y = y - (hover and 3 or 0), w = w, h = h, action = action, disabled = not active, hotkey = hotkey}
end

local function effects(effects_list)
  if #effects_list == 0 then return L("Nothing") end
  local parts = {}
  for _, e in ipairs(effects_list) do
    if e.type == "next_mult" then parts[#parts + 1] = L("NEXT %d x%d", e.coins, e.amount) goto continue end
    if e.type == "next_odds" then parts[#parts + 1] = L("NEXT %d +%d%%", e.coins, math.floor(e.amount * 100 + .5)) goto continue end
    if e.type == "all_odds" then parts[#parts + 1] = L("ALL +%d%%", math.floor(e.amount * 100 + .5)) goto continue end
    if e.type == "fortune_odds" then parts[#parts + 1] = L("FORTUNE +%d%%", math.floor(e.amount * 100 + .5)) goto continue end
    if e.type == "type_buff" then parts[#parts + 1] = L("NEXT %d %s", e.coins, L(e.kind:upper())) goto continue end
    if e.type == "fetch_best" then parts[#parts + 1] = L("FETCH BEST") goto continue end
    if e.type == "gold_loss" then parts[#parts + 1] = L("-%d GOLD", e.amount) goto continue end
    if e.type == "peek" then parts[#parts + 1] = L("PEEK") goto continue end
    if e.type == "bank_discard" then parts[#parts + 1] = L("DISCARD 1 OF NEXT 3") goto continue end
    if e.type == "extra_exchange" then parts[#parts + 1] = L("+%d EXCHANGE", e.amount) goto continue end
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
    if effect.type == "score" then parts[#parts + 1] = amount == 1 and L("Score 1 point") or L("Score %d points", amount)
    elseif effect.type == "gold" then parts[#parts + 1] = L("Gain %d gold", amount)
    elseif effect.type == "gold_loss" then parts[#parts + 1] = L("Lose up to %d gold", amount)
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
    elseif effect.type == "all_odds" then parts[#parts + 1] = L("All coins +%d%% Heads this level", math.floor(amount * 100 + .5))
    elseif effect.type == "fortune_odds" then parts[#parts + 1] = L("Fortune coins gain +%d%% Heads for the run", math.floor(amount * 100 + .5))
    elseif effect.type == "type_buff" then parts[#parts + 1] = L("Prepare the next %d %s coins", effect.coins, L(effect.kind:upper()))
    elseif effect.type == "fetch_best" then parts[#parts + 1] = L("Return the highest-scoring played coin")
    elseif effect.type == "peek" then parts[#parts + 1] = L("Look at the next two coins")
    elseif effect.type == "bank_discard" then parts[#parts + 1] = L("Discard one of the next three coins")
    elseif effect.type == "extra_exchange" then parts[#parts + 1] = L("One more exchange this level")
    elseif effect.type == "amplify" then parts[#parts + 1] = L("Buffs last 1 coin longer and get stronger")
    elseif effect.type == "combo_bonus" then parts[#parts + 1] = L("Combo grows %d extra step", amount)
    elseif effect.type == "combo_shield" then parts[#parts + 1] = L("The next combo break is prevented")
    elseif effect.type == "next_swap" then parts[#parts + 1] = L("Next coin uses its other side")
    elseif effect.type == "next_heads" then parts[#parts + 1] = L("Next coin lands Heads")
    end
  end
  return table.concat(parts, "; ")
end

local function coin_hover(id, x, y, w, h, probability, locked, upgrade_id)
  local demo_odds = ui.game and ui.game.sandbox and ui.game.sandbox.odds and ui.game.sandbox.odds[id]
  local upgrade = upgrade_id and catalog[id].upgrades and catalog[id].upgrades[upgrade_id]
  local tie = ui.game and Game.tie_probability(ui.game, {id = id}) or catalog[id].tie_probability or 0
  local base_probability = demo_odds and demo_odds.heads or catalog[id].probability
  local info = {id = id, probability = probability or base_probability + (upgrade and upgrade.heads_probability or 0),
    tie_probability = tie, locked = locked, upgrade = upgrade_id}
  ui.regions[#ui.regions + 1] = {x = x, y = y, w = w, h = h, coin = info}
  local mx, my = ui.mouse()
  if mx >= x and mx <= x + w and my >= y and my <= y + h then ui.hovered_coin = info end
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
local function icon_button(label, icon, x, y, w, h, tint, action, enabled, hotkey)
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
  ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = w, h = h, action = action, disabled = not active, hotkey = hotkey}
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
  if back_action then button(back_label, 1120, 56, 100, 34, C.panel_light, back_action, nil, "B") end
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
    button("CANCEL", 660, 454, 200, 52, C.panel_light, function() ui.confirm = nil end, nil, "B")
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
  local info = {title = title, body = body}
  ui.regions[#ui.regions + 1] = {x = x, y = y, w = w, h = h, text = info}
  local mx, my = ui.mouse()
  if mx >= x and mx <= x + w and my >= y and my <= y + h then ui.hovered_text = info end
end

local function draw_text_tooltip()
  local tip = ui.hovered_text
  if not tip then return end
  local mx, my = ui.pointer()
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
  local game = ui.game
  local encounter_visible = game and (game.phase == "ENCOUNTER"
    or game.phase == "GAME_OVER" and not game.over_seen)
  if not ui.hovered_coin or encounter_visible then return end
  local coin = catalog[ui.hovered_coin.id]
  local upgrade = ui.hovered_coin.upgrade and coin.upgrades and coin.upgrades[ui.hovered_coin.upgrade]
  local mx, my = ui.pointer()
  local w = 470
  local tie = ui.hovered_coin.tie_probability or 0
  local heads = math.floor(ui.hovered_coin.probability * 100 + .5)
  local rows = {
    {label = L("HEADS"), chance = heads, detail = coin.heads_description or effect_description(coin.heads), tint = C.blue},
  }
  if tie > 0 then
    rows[#rows + 1] = {label = L("EDGE"), chance = math.floor(tie * 100 + .5),
      detail = effect_description(Game.tie_effects(ui.hovered_coin.id)), tint = C.purple}
  end
  rows[#rows + 1] = {label = L("TAILS"), chance = math.floor((1 - ui.hovered_coin.probability - tie) * 100 + .5),
    detail = effect_description(coin.tails), tint = C.red}

  local header_height = coin.coin_types and 110 or 94
  local h = header_height
  for _, row in ipairs(rows) do
    local _, lines = ui.f16:getWrap(row.detail, w - 52)
    row.height = 38 + #lines * 18
    h = h + row.height + 7
  end
  local description = coin.heads_description and nil or coin.description
  local description_lines
  if description then
    _, description_lines = ui.f16:getWrap(description, w - 48)
    h = h + 40 + #description_lines * 18
  end
  if (coin.energy_cost or 0) > 0 then h = h + 24 end
  local upgrade_lines
  if upgrade then
    _, upgrade_lines = ui.f16:getWrap(L(upgrade.description), w - 48)
    h = h + 42 + #upgrade_lines * 18
  end
  if ui.hovered_coin.locked then
    local _, locked_lines = ui.f16:getWrap(L("LOCKED  -  BUY IT IN THE SHOP TO UNLOCK"), w - 48)
    h = h + 14 + #locked_lines * 18
  end
  h = h + 8
  local x, y
  if ui.screen == "sets" then
    -- Keep details out of the coin catalog so the panel never blocks coins being browsed.
    x = 24
    y = math.max(12, math.min(my + 18, 788 - h - 12))
  else
    x = mx > 640 and mx - w - 18 or mx + 18
    y = my > 400 and my - h - 18 or my + 18
    x = math.max(12, math.min(x, 1280 - w - 12))
    y = math.max(12, math.min(y, 788 - h - 12))
  end
  box(x, y, w, h, C.ink)
  outline(x, y, w, h, C.gold)
  coin_image(ui.hovered_coin.id, x + 14, y + 13, 66)
  text(coin.name:upper(), x + 94, y + 16, ui.f20, C.face)
  local rarity = L(RARITY_NAME[coin.rarity]):upper()
  text(rarity, x + 94, y + 47, ui.f16, C.rarity[coin.rarity])
  if coin.coin_types then
    local names = {}
    for _, kind in ipairs(coin.coin_types) do
      local name = L(kind:upper())
      if kind == "fortune" and ui.game and (ui.game.fortune_bonus or 0) > 0 then
        name = name .. string.format(" +%d%%", math.floor(ui.game.fortune_bonus * 100 + .5))
      end
      names[#names + 1] = name
    end
    text(L("TYPE: %s", table.concat(names, " / ")), x + 94, y + 69, ui.f16, C.muted)
  end

  local row_y = y + header_height
  for _, row in ipairs(rows) do
    box(x + 12, row_y, w - 24, row.height, C.slot)
    color(row.tint)
    love.graphics.rectangle("fill", x + 12, row_y, 4, row.height)
    text(row.label, x + 26, row_y + 7, ui.f16, row.tint)
    text(row.chance .. "%", x + w - 78, row_y + 7, ui.f16, row.tint)
    love.graphics.setFont(ui.f16)
    color(C.face)
    love.graphics.printf(row.detail, x + 26, row_y + 29, w - 52)
    row_y = row_y + row.height + 7
  end
  if description then
    text("DETAILS", x + 24, row_y + 7, ui.f16, C.gold)
    love.graphics.setFont(ui.f16)
    color(C.muted)
    love.graphics.printf(description, x + 24, row_y + 28, w - 48)
    row_y = row_y + 40 + #description_lines * 18
  end
  if (coin.energy_cost or 0) > 0 then
    text(L("COSTS %d ENERGY", coin.energy_cost), x + 24, row_y + 4, ui.f16, C.gold)
    row_y = row_y + 24
  end
  if upgrade then
    text(L("UPGRADE: %s", L(upgrade.name):upper()), x + 24, row_y + 4, ui.f16, C.gold)
    love.graphics.setFont(ui.f16)
    color(C.face)
    love.graphics.printf(L(upgrade.description), x + 24, row_y + 25, w - 48)
    row_y = row_y + 42 + #upgrade_lines * 18
  end
  if ui.hovered_coin.locked then
    love.graphics.setFont(ui.f16)
    color(C.orange)
    love.graphics.printf(L("LOCKED  -  BUY IT IN THE SHOP TO UNLOCK"), x + 24, row_y + 8, w - 48)
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
    local tint = outcome == "Heads" and C.blue or outcome == "Tie" and C.purple or C.red
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
