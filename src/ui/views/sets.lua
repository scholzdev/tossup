-- Coin Sets: every character with all of its coins. Build up to three sets of up to 10 coins per
-- character. Locked coins are unlocked by buying them in the shop during a run.
local Game = require("src.game")
local Profile = require("src.profile")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_hover = D.coin_image, D.coin_hover

local SLOT, SLOT_GAP = 64, 14
local GRID_X, CELL = 566, 84

local function padlock(cx, cy)
  color(C.face)
  love.graphics.setLineWidth(3)
  love.graphics.arc("line", "open", cx, cy - 2, 6, math.pi, 2 * math.pi)
  love.graphics.setLineWidth(1)
  love.graphics.rectangle("fill", cx - 9, cy - 2, 18, 14, 2)
  color(C.ink)
  love.graphics.circle("fill", cx, cy + 5, 2)
end

local function count_in(coins, id)
  local n = 0
  for _, owned in ipairs(coins) do if owned == id then n = n + 1 end end
  return n
end

local function draw_sets()
  color(C.felt_dark)
  love.graphics.rectangle("fill", -2000, -2000, 5280, 4800)
  box(400, -10, 480, 80, C.gold)
  centered("COIN SETS", 400, 22, 480, ui.f32, C.ink)
  button("X", 24, 24, 64, 64, C.gold, function() A.go("title") end)

  -- every character
  for i, id in ipairs(ui.character_order) do
    local selected = id == ui.sets_character
    button(ui.characters[id].name:sub(5):upper(), 320 + (i - 1) * 220, 100, 200, 50,
      selected and C.gold or C.panel_light, function() A.sets_pick_character(id) end)
  end

  local character_id = ui.sets_character
  local def = ui.characters[character_id]
  local sets = Profile.sets(ui.profile, character_id)
  local set = {name = sets[ui.sets_index].name, coins = A.set_draft()}
  local dirty = A.set_dirty()

  -- left: the set being edited
  box(60, 175, 470, 540, C.ink)
  outline(60, 175, 470, 540, C.panel_light)
  for i = 1, Profile.SET_COUNT do
    button(sets[i].name, 76 + (i - 1) * 146, 190, 138, 44, ui.sets_index == i and C.blue or C.panel_light,
      function() A.sets_pick_set(i) end)
  end
  centered(#set.coins .. " / " .. Game.START_MAX .. " COINS", 60, 252, 470, ui.f20, C.gold)
  local x0 = 60 + (470 - (5 * (SLOT + SLOT_GAP) - SLOT_GAP)) / 2
  for i = 1, Game.START_MAX do
    local x = x0 + ((i - 1) % 5) * (SLOT + SLOT_GAP)
    local y = 292 + math.floor((i - 1) / 5) * (SLOT + SLOT_GAP + 8)
    box(x - 4, y - 4, SLOT + 8, SLOT + 8, C.slot)
    outline(x - 4, y - 4, SLOT + 8, SLOT + 8, C.panel_light)
    local id = set.coins[i]
    if id then
      coin_image(id, x, y, SLOT)
      coin_hover(id, x, y, SLOT, SLOT)
      ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = SLOT, h = SLOT, action = function() A.remove_coin_from_set(i) end}
    end
  end
  centered(dirty and "UNSAVED CHANGES" or "CLICK A COIN HERE TO REMOVE IT", 60, 458, 470, ui.f16,
    dirty and C.orange or C.muted)
  button(dirty and "SAVE SET" or "SAVED", 96, 490, 398, 50, dirty and C.blue or C.panel_light, A.save_set, dirty)
  button("CLEAR SET", 96, 560, 398, 44, C.red, A.clear_set, #set.coins > 0)
  centered("MAX " .. Game.MAX_COPIES .. " OF THE SAME COIN  -  NORMAL: UP TO " .. Game.START_MAX, 60, 676, 470, ui.f16, C.muted)

  -- right: all of this character's coins
  box(550, 175, 690, 540, C.ink)
  outline(550, 175, 690, 540, C.panel_light)
  centered(def.name:upper() .. "  -  CLICK A COIN TO ADD IT  -  LOCKED COINS COME FROM THE SHOP", 550, 188, 690, ui.f16, C.gold)
  local entries = {}
  for _, id in ipairs(def.pool) do entries[#entries + 1] = {id = id} end
  for _, entry in ipairs(def.locked or {}) do
    entries[#entries + 1] = {id = entry[1], cost = entry[2], locked = not Profile.is_unlocked(ui.profile, character_id, entry[1])}
  end
  local columns = 8
  for i, entry in ipairs(entries) do
    local x = GRID_X + ((i - 1) % columns) * CELL
    local y = 220 + math.floor((i - 1) / columns) * (SLOT + 38)
    coin_image(entry.id, x, y, SLOT)
    if entry.locked then
      color(C.slot, .7)
      love.graphics.circle("fill", x + SLOT / 2, y + SLOT / 2, SLOT / 2)
      padlock(x + SLOT / 2, y + SLOT / 2 - 4)
      centered("SHOP", x, y + SLOT + 2, SLOT, ui.f16, C.muted)
    else
      local n = count_in(set.coins, entry.id)
      centered(n > 0 and ("x" .. n) or "", x, y + SLOT + 2, SLOT, ui.f16, C.green)
      ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = SLOT, h = SLOT, action = function() A.add_coin_to_set(entry.id) end}
    end
    coin_hover(entry.id, x, y, SLOT, SLOT, nil, entry.locked)
  end
end

return draw_sets
