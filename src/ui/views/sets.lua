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
  D.frame(D.title("sets"), "BACK", function() A.go("title") end)

  -- every character
  for i, id in ipairs(ui.character_order) do
    local selected = id == ui.sets_character
    button(D.L(({blade = "BLADE", seer = "SEER", trader = "TRADER"})[id]), 340 + (i - 1) * 210, 148, 190, 44,
      selected and C.gold or C.panel_light, function() A.sets_pick_character(id) end, Profile.character_unlocked(ui.profile, id))
  end

  local character_id = ui.sets_character
  local def = ui.characters[character_id]
  local sets = Profile.sets(ui.profile, character_id)
  local set = {name = sets[ui.sets_index].name, coins = A.set_draft()}
  local dirty = A.set_dirty()

  -- left: the set being edited
  box(70, 212, 430, 528, C.panel_dk)
  outline(70, 212, 430, 528, C.line)
  for i = 1, Profile.SET_COUNT do
    button(sets[i].name, 84 + (i - 1) * 134, 226, 126, 40, ui.sets_index == i and C.blue or C.panel_light,
      function() A.sets_pick_set(i) end)
  end
  centered(D.L("%d / %d COINS", #set.coins, Game.START_MAX), 70, 280, 430, ui.f20, C.gold)
  local x0 = 70 + (430 - (5 * (SLOT + SLOT_GAP) - SLOT_GAP)) / 2
  for i = 1, Game.START_MAX do
    local x = x0 + ((i - 1) % 5) * (SLOT + SLOT_GAP)
    local y = 322 + math.floor((i - 1) / 5) * (SLOT + SLOT_GAP + 8)
    box(x - 4, y - 4, SLOT + 8, SLOT + 8, C.slot_dk)
    outline(x - 4, y - 4, SLOT + 8, SLOT + 8, C.line)
    local id = set.coins[i]
    if id then
      coin_image(id, x, y, SLOT)
      coin_hover(id, x, y, SLOT, SLOT)
      ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = SLOT, h = SLOT, action = function() A.remove_coin_from_set(i) end}
    end
  end
  centered(dirty and "UNSAVED CHANGES" or "CLICK A COIN HERE TO REMOVE IT", 70, 496, 430, ui.f16,
    dirty and C.orange or C.muted)
  button(dirty and "SAVE SET" or "SAVED", 100, 530, 370, 48, dirty and C.blue or C.panel_light, A.save_set, dirty)
  button("CLEAR SET", 100, 592, 370, 44, C.red, A.clear_set, #set.coins > 0)
  centered(D.L("MAX %d OF THE SAME COIN  -  NORMAL: UP TO %d", Game.MAX_COPIES, Game.START_MAX), 70, 702, 430,
    ui.f16, C.muted)

  -- right: all of this character's coins
  box(520, 212, 700, 528, C.panel_dk)
  outline(520, 212, 700, 528, C.line)
  centered(D.L("%s  -  CLICK A COIN TO ADD IT  -  LOCKED COINS COME FROM THE SHOP", def.name:upper()), 520, 226, 700,
    ui.f16, C.gold)
  local entries = {}
  for _, id in ipairs(def.pool) do entries[#entries + 1] = {id = id} end
  for _, entry in ipairs(def.locked or {}) do
    entries[#entries + 1] = {id = entry[1], cost = entry[2], locked = not Profile.is_unlocked(ui.profile, character_id, entry[1])}
  end
  local columns, step = 8, 82
  local gx = 520 + (700 - ((columns - 1) * step + SLOT)) / 2
  for i, entry in ipairs(entries) do
    local x = gx + ((i - 1) % columns) * step
    local y = 262 + math.floor((i - 1) / columns) * (SLOT + 40)
    coin_image(entry.id, x, y, SLOT)
    if entry.locked then
      color(C.slot_dk, .7)
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
