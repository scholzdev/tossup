local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_face, coin_hover, effects, effect_description =
  D.coin_image, D.coin_face, D.coin_hover, D.effects, D.effect_description
local Items = require("src.items")
local catalog, characters = ui.catalog, ui.characters

-- The coins shown in the left panel: the next VISIBLE coins of the bank (or the opening hand).
local function bank_coins()
  local g = ui.game
  local uids = g.mulligan and g.mulligan.hand or g.encounter.queue
  local list = {}
  for i = 1, math.min(Game.VISIBLE, #uids) do list[i] = Game.get_coin(g, uids[i]) end
  return list
end

-- Opening hand: look at the coins, discard the ones you do not want (1 energy each), then start.
local function draw_mulligan()
  local g = ui.game
  local hand = g.mulligan.hand
  box(292, 160, 960, 504, C.panel)
  outline(292, 160, 960, 504, C.gold)
  centered("OPENING HAND", 292, 178, 960, ui.f32, C.gold)
  centered("DISCARD COINS YOU DO NOT WANT  -  FREE  -  THEY STAY OUT FOR THE LEVEL", 292, 220, 960, ui.f16, C.muted)
  local w, gap = 168, 16
  local x0 = 292 + (960 - (#hand * w + (#hand - 1) * gap)) / 2
  for i, uid in ipairs(hand) do
    local owned = Game.get_coin(g, uid)
    local def = catalog[owned.id]
    local x, y = x0 + (i - 1) * (w + gap), 262
    local marked = ui.marked[uid]
    box(x, y, w, 330, marked and C.panel_light or C.ink)
    outline(x, y, w, 330, marked and C.red or i <= Game.VISIBLE and C.gold or C.panel_light)
    coin_image(owned.id, x + 34, y + 12, 100)
    centered(def.name:upper(), x, y + 118, w, ui.f20, C.face)
    centered(math.floor(Game.probability(g, owned) * 100 + .5) .. "% HEADS", x, y + 146, w, ui.f16, C.gold)
    text("H " .. effects(def.heads), x + 10, y + 180, ui.f16, C.blue)
    text("T " .. effects(def.tails), x + 10, y + 204, ui.f16, C.red)
    local cost = def.energy_cost or 0
    if cost > 0 then text("ENERGY COST " .. cost, x + 10, y + 232, ui.f16, C.orange) end
    if marked then centered("MARKED TO DISCARD", x, y + 280, w, ui.f16, C.red)
    elseif i <= Game.VISIBLE then centered("PLAYS NEXT", x, y + 280, w, ui.f16, C.gold) end
    coin_hover(owned.id, x, y, w, 330, Game.probability(g, owned))
    ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = w, h = 330, action = function() A.toggle_mark(uid) end}
  end
  centered("CLICK COINS TO MARK THEM, THEN PRESS DISCARD  -  GOLD FRAMES PLAY FIRST", 292, 612, 960, ui.f16, C.muted)

  box(28, 680, 1224, 90, C.ink)
  outline(28, 680, 1224, 90, C.panel_light)
  text("OPENING HAND", 45, 694, ui.f20, C.gold)
  text("Mark the coins you do not want.", 45, 728, ui.f16, C.muted)
  local marked_count = A.marked_count()
  D.icon_button(marked_count > 0 and ("DISCARD " .. marked_count) or "DISCARD", ui.ui_images.discard, 290, 692, 200, 64,
    C.red, A.discard_marked, marked_count > 0 and marked_count < #hand)
  D.icon_button("START LEVEL", ui.ui_images.start_level, 510, 692, 260, 64, C.green, function() Game.mulligan_done(g) end)
end

local function draw_flip_animation()
  if not ui.flip_animation then return end
  local progress = math.min(1, ui.flip_animation.elapsed / ui.flip_animation.duration)
  -- quartic ease-out so the spin slows to a tense stop; even half-turn = Tails up, odd = Heads up
  local turns = ui.flip_animation.outcome == "Heads" and 9 or 10
  local phase = turns * (1 - (1 - progress) ^ 4)
  local squash = math.max(.06, math.abs(math.cos(phase * math.pi)))
  local heads = math.floor(phase + .5) % 2 == 1 -- the side changes at the thin edge-on moments, not at full width
  local lift = 90 * math.sin(math.min(1, progress / .75) * math.pi)
  local word = heads and "HEADS" or "TAILS"
  love.graphics.push()
  love.graphics.translate(619, 390 - lift)
  love.graphics.scale(squash, 1)
  color(C.white)
  local image = ui.coin_images[ui.flip_animation.id]
  love.graphics.draw(image, -150, -150, 0, 300 / image:getWidth(), 300 / image:getHeight())
  color(C.ink)
  love.graphics.rectangle("fill", -90, 76, 180, 44, 6)
  text(word, -ui.f32:getWidth(word) / 2, 82, ui.f32, heads and C.blue or C.red)
  love.graphics.pop()
end

local function draw_encounter()
  local e = ui.game.encounter
  box(16, 16, 1248, 768, C.felt_dark)
  outline(16, 16, 1248, 768, C.panel_light)

  box(28, 28, 1224, 118, C.ink)
  outline(28, 28, 1224, 118, C.panel_light)
  -- left: where you are
  text("TOSSUP", 43, 37, ui.f32, C.gold)
  text("LEVEL " .. ui.game.encounter_index .. " / 4", 44, 78, ui.f16, C.muted)
  text(e.boss and "THE HOUSE" or e.name:upper(), 44, 100, ui.f20, e.boss and C.red or C.face)
  -- centre: the number that matters, points scored against the quota
  local met = e.quota <= 0
  -- one caption line above the number: a status when there is one, otherwise just "POINTS"
  local caption, caption_color = "POINTS", C.muted
  if e.cleared then caption, caption_color = "QUOTA MET  -  EXTRA POINTS PAY GOLD", C.green
  elseif e.boss then caption, caption_color = "THE HOUSE  -  EVERY 5TH FLIP IS INVERTED", C.red end
  centered(caption, 330, 36, 580, ui.f16, caption_color)
  centered(e.scored .. " / " .. e.max_quota, 330, 56, 580, ui.f48, met and C.green or C.gold)
  color(C.slot)
  love.graphics.rectangle("fill", 330, 112, 580, 22, 4)
  color(met and C.green or C.gold)
  love.graphics.rectangle("fill", 330, 112, 580 * math.min(1, e.scored / e.max_quota), 22, 4)
  outline(330, 112, 580, 22, C.panel_light, 4)
  if e.cleared then
    D.icon_button("OPEN SHOP", ui.ui_images.open_shop, 940, 108, 192, 30, C.green, A.open_shop,
      not ui.flip_animation and not ui.game.pending and not ui.game.mulligan)
  end
  -- right: what you have left
  local stats = {
    {"COINS", tostring(Game.coins_left(ui.game)), Game.coins_left(ui.game) <= 2 and C.red or C.face},
    {"GOLD", tostring(ui.game.player.gold), C.gold},
    {"ENERGY", tostring(ui.game.player.energy), C.blue},
  }
  local stat_icons = {"coins_left", "gold", "energy"}
  for i, stat in ipairs(stats) do
    local x = 940 + (i - 1) * 100
    box(x, 38, 92, 62, C.panel)
    text(stat[1], x + 8, 43, ui.f16, C.muted)
    text(stat[2], x + 8, 64, ui.f32, stat[3])
    D.image_at(ui.ui_images[stat_icons[i]], x + 60, 42, 26)
  end
  button("MENU", 1140, 108, 92, 30, C.panel_light, A.open_menu)

  local remaining = bank_coins()
  local deck_total = #ui.game.coins
  local queue_count = #(ui.game.mulligan and ui.game.mulligan.hand or e.queue)
  box(28, 160, 250, 504, C.ink)
  outline(28, 160, 250, 504, C.panel_light)
  text("COIN BANK", 42, 175, ui.f20, C.gold)
  text("NEXT " .. #remaining .. " COINS", 43, 207, ui.f16, C.face)
  for i = 1, Game.VISIBLE do
    local x, y = 41, 265 + (i - 1) * 75
    local owned = remaining[i] -- flipped and discarded coins drop off the list
    local current = owned and ui.game.dealt and owned.uid == ui.game.dealt.uid
    local marked = owned and ui.marked[owned.uid]
    box(x, y, 224, 64, marked and C.panel_light or owned and C.panel or C.slot)
    outline(x, y, 224, 64, marked and C.red or current and C.gold or owned and C.panel_light or C.ink)
    if owned then
      if marked then
        color(C.red)
        love.graphics.rectangle("fill", x + 140, y - 9, 76, 18, 4)
        centered("DISCARD", x + 140, y - 9, 76, ui.f16, C.ink)
      end
      local cost = catalog[owned.id].energy_cost or 0
      if cost > 0 then text("E" .. cost, x + 196, y + 36, ui.f16, C.orange) end
      if ui.game.dealt and not ui.flip_animation and not ui.holding and not ui.game.mulligan then
        ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = 224, h = 64, action = function() A.toggle_mark(owned.uid) end}
      end
      if current and not marked then -- tab on the card's top edge
        color(C.gold)
        love.graphics.rectangle("fill", x + 140, y - 9, 76, 18, 4)
        centered("CURRENT", x + 140, y - 9, 76, ui.f16, C.ink)
      end
      coin_image(owned.id, x + 4, y + 4, 56)
      text(catalog[owned.id].name:upper(), x + 63, y + 7, ui.f20, C.face)
      text(math.floor(Game.probability(ui.game, owned) * 100 + .5) .. "% HEADS",
        x + 64, y + 36, ui.f16, C.gold)
    else
      centered("EMPTY SLOT", x, y + 20, 224, ui.f16, C.muted)
    end
  end
  local y = 265 + Game.VISIBLE * 75 + 8
  text("IN BANK      " .. queue_count, 43, y, ui.f16, C.face)
  text("DRAW PILE    " .. #e.pile, 43, y + 24, ui.f16, C.face)
  text("DISCARDED    " .. e.discards, 43, y + 48, ui.f16, C.muted)
  text("DECK         " .. deck_total .. " / " .. Game.DECK_MAX, 43, y + 72, ui.f16, C.muted)

  box(292, 160, 654, 504, C.panel)
  outline(292, 160, 654, 504, C.gold)
  local result = not ui.flip_animation and (ui.game.pending or ui.holding and ui.game.last_result or ui.game.dealt or ui.game.last_result)
  local item = result and Game.get_coin(ui.game, result.uid)
  local outcome = result and (result.final or result.result)
  centered(ui.flip_animation and "FLIPPING" or ui.game.pending and "CURRENT FLIP" or
    ui.game.dealt and not ui.holding and "DEALT COIN" or item and "LAST FLIP" or "NO COIN", 310, 179, 618, ui.f20, C.gold)
  color(C.felt_dark)
  love.graphics.circle("fill", 619, 390, 180)
  color(C.panel_light)
  love.graphics.setLineWidth(3)
  love.graphics.circle("line", 619, 390, 180)
  love.graphics.setLineWidth(1)
  if item then
    coin_face(619, 390, 115, nil, true, item.id)
  elseif not ui.flip_animation then
    coin_image("back", 469, 240, 300)
  end
  draw_flip_animation()
  local shown_cost = item and catalog[item.id].energy_cost or 0
  if shown_cost > 0 and not ui.flip_animation then text("ENERGY COST " .. shown_cost, 310, 179, ui.f16, C.orange) end
  centered(item and catalog[item.id].name:upper() or ui.flip_animation and "DRAWING..." or
    (not ui.game.mulligan and "NO COINS LEFT" or "MYSTERY COIN"), 310, 578, 618, ui.f32, C.face)
  centered(item and ((outcome and outcome:upper() .. " / " or "") ..
    math.floor(result.probability * 100 + .5) .. "% HEADS") or
    "ONE COIN AT A TIME", 310, 617, 618, ui.f16,
    outcome and (outcome == "Heads" and C.blue or C.red) or C.muted)
  if result and result.altered and result.raw and not ui.flip_animation then
    centered("ROLLED " .. result.raw:upper() .. "  >  " .. outcome:upper() .. "  (" .. result.altered .. ")",
      310, 642, 618, ui.f16, C.orange)
  end

  box(960, 160, 292, 504, C.ink)
  outline(960, 160, 292, 504, C.panel_light)
  text("CURRENT COIN", 974, 175, ui.f20, C.gold)
  if item then
    local coin = catalog[item.id]
    local chance = result.probability
    local mx, my = ui.mouse()
    text(coin.name:upper(), 975, 210, ui.f32, C.face)
    text(coin.description, 975, 253, ui.f16, C.muted)
    text("HEADS " .. math.floor(chance * 100 + .5) .. "%", 975, 287, ui.f16, C.blue)
    text("TAILS " .. math.floor((1 - chance) * 100 + .5) .. "%", 1135, 287, ui.f16, C.red)
    color(C.blue)
    love.graphics.rectangle("fill", 975, 314, 262 * chance, 11)
    color(C.red)
    love.graphics.rectangle("fill", 975 + 262 * chance, 314, 262 * (1 - chance), 11)
    box(973, 343, 266, 76, C.panel)
    outline(973, 343, 266, 76, C.blue)
    text("HEADS", 984, 350, ui.f16, C.blue)
    text(effect_description(coin.heads), 984, 379, ui.f16, C.face)
    box(973, 429, 266, 76, C.panel)
    outline(973, 429, 266, 76, C.red)
    text("TAILS", 984, 436, ui.f16, C.red)
    text(effect_description(coin.tails), 984, 465, ui.f16, C.face)
    local result_color = not outcome and C.muted or outcome == "Heads" and C.blue or C.red
    box(973, 527, 266, 124, C.panel)
    outline(973, 527, 266, 124, result_color)
    centered(outcome and outcome:upper() or "READY", 980, 546, 252, ui.f32, result_color)
    local note, note_color = "EFFECT APPLIED", C.muted
    if ui.game.dealt and not ui.holding then note = "FLIP IT OR DISCARD"
    elseif ui.game.pending then note = "APPLYING..."
    elseif result and result.gained and result.gained > 0 then note, note_color = "+" .. result.gained .. " POINTS", C.green
    elseif result and result.penalty and result.penalty > 0 then note, note_color = "QUOTA +" .. result.penalty, C.red
    elseif result and result.gained then note = "NO POINTS" end
    centered(note, 980, 600, 252, ui.f20, note_color)
  else
    text(ui.flip_animation and "FLIPPING..." or "UNKNOWN", 975, 213, ui.f32, C.face)
    text("Flip to reveal a coin", 975, 261, ui.f16, C.muted)
    text("from your stack.", 975, 283, ui.f16, C.muted)
    box(973, 343, 266, 76, C.slot)
    outline(973, 343, 266, 76, C.panel_light)
    text("HEADS", 984, 350, ui.f16, C.blue)
    text("?", 984, 379, ui.f16, C.muted)
    box(973, 429, 266, 76, C.slot)
    outline(973, 429, 266, 76, C.panel_light)
    text("TAILS", 984, 436, ui.f16, C.red)
    text("?", 984, 465, ui.f16, C.muted)
    box(973, 527, 266, 124, C.slot)
    outline(973, 527, 266, 124, C.panel_light)
    centered("AWAITING FLIP", 980, 571, 252, ui.f20, C.muted)
  end

  box(28, 680, 1224, 90, C.ink)
  outline(28, 680, 1224, 90, C.panel_light)
  text(ui.flip_animation and "COIN IN MOTION" or ui.game.pending and "COIN FLIPPED" or
    "YOUR MOVE", 45, 694, ui.f20, C.gold)
  local hint = ui.holding and "Click for the next coin." or "Flip it, or mark coins and discard."
  if e.cleared then hint = "Keep going for gold, or open the shop." end
  if not ui.game.dealt and not ui.game.mulligan and not ui.game.pending and not ui.flip_animation and not ui.holding then
    hint = Game.can_exchange(ui.game) and "No coins left: exchange?" or "No coins left."
  end
  if ui.game.dealt and not ui.holding and not Game.can_flip(ui.game) then hint = "Too little energy: discard it." end
  if ui.game.peek then
    local names = {}
    for i, uid in ipairs(ui.game.peek) do names[i] = catalog[Game.get_coin(ui.game, uid).id].name:upper() end
    hint = "NEXT: " .. table.concat(names, ", ")
  end
  local mx, my = ui.mouse()
  local usable = Items.can_use(ui.game) and not ui.flip_animation and not ui.holding
  for slot = 1, Items.MAX do
    local x = 790 + (slot - 1) * 152
    local id = ui.game.items[slot]
    if id then
      local def = ui.item_catalog[id]
      local hover = usable and mx >= x and mx <= x + 140 and my >= 692 and my <= 756
      box(x, 692 + (hover and -3 or 0), 140, 64, usable and C.panel_light or C.slot)
      outline(x, 692 + (hover and -3 or 0), 140, 64, usable and C.orange or C.panel_light)
      D.image_at(ui.item_images[id], x + 8, 702 + (hover and -3 or 0), 44)
      text(def.short, x + 58, 716 + (hover and -3 or 0), ui.f16, usable and C.face or C.muted)
      if usable then ui.buttons[#ui.buttons + 1] = {x = x, y = 692, w = 140, h = 64, action = function() A.use_item(slot) end} end
      if mx >= x and mx <= x + 140 and my >= 692 and my <= 756 then hint = def.description end
    else
      box(x, 692, 140, 64, C.slot)
      outline(x, 692, 140, 64, C.panel_light)
      centered("ITEM", x, 714, 140, ui.f16, C.muted)
    end
  end
  text(hint, 45, 728, ui.f16, C.muted)
  if ui.game.dealt and not ui.flip_animation and not ui.holding then
    local n = A.marked_count()
    D.icon_button(n > 0 and ("DISCARD " .. n) or "DISCARD", ui.ui_images.discard, 290, 692, 200, 64, C.red,
      A.discard_marked, n > 0)
  end
  local empty_stack = not ui.game.dealt and not ui.game.mulligan and not ui.game.pending
    and not ui.flip_animation and not ui.holding
  if empty_stack then
    -- no coins left: exchange two played Normal coins for some back, give up, or (cleared) open the shop
    if Game.can_exchange(ui.game) then
      D.icon_button("PAY " .. Game.exchange_cost(ui.game) .. " > " .. Game.EXCHANGE_GAIN .. " COINS", ui.ui_images.exchange,
        510, 692, 260, 64, C.green, A.exchange)
    end
    if not e.cleared then D.icon_button("GIVE UP", ui.ui_images.give_up, 290, 692, 200, 64, C.red, A.give_up) end
  else
    local flip_label = ui.flip_animation and "FLIPPING..." or ui.holding and "NEXT COIN" or "FLIP"
    local can_act = ui.game.dealt ~= nil and not ui.flip_animation and (ui.holding or not ui.game.pending)
    if can_act and not ui.holding and not Game.can_flip(ui.game) then
      flip_label = "NEED " .. Game.flip_cost(ui.game, ui.game.dealt.uid) .. " ENERGY"
      can_act = false
    end
    D.icon_button(flip_label, ui.holding and ui.ui_images.next_coin or ui.ui_images.flip, 510, 692, 260, 64, C.blue,
      A.next_or_flip, can_act)
  end
  if ui.game.mulligan then draw_mulligan() end -- covers the play area and takes over the bottom bar
end

return draw_encounter
