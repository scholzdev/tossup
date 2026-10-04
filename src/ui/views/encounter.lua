local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local draw_run_modifiers = require("src.ui.views.run_modifiers")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_face, coin_hover, effect_description =
  D.coin_image, D.coin_face, D.coin_hover, D.effect_description
local Items = require("src.items")
local catalog = ui.catalog

-- The entire remaining bank, in play order: queued coins followed by the draw pile.
local function bank_coins()
  local g = ui.game
  local uids = {}
  for _, uid in ipairs(g.encounter.queue) do uids[#uids + 1] = uid end
  for _, uid in ipairs(g.encounter.pile) do uids[#uids + 1] = uid end
  local list = {}
  for i = 1, math.min(Game.DECK_MAX, #uids) do list[i] = Game.get_coin(g, uids[i]) end
  return list
end

local function draw_flip_animation()
  if not ui.flip_animation then return end
  local progress = math.min(1, ui.flip_animation.elapsed / ui.flip_animation.duration)
  -- quartic ease-out so the spin slows to a tense stop; even half-turn = Tails up, odd = Heads up
  local edge = ui.flip_animation.outcome == "Tie"
  local turns = edge and 9.5 or ui.flip_animation.outcome == "Heads" and 9 or 10
  local phase = turns * (1 - (1 - progress) ^ 4)
  local squash = math.max(.06, math.abs(math.cos(phase * math.pi)))
  local heads = math.floor(phase + .5) % 2 == 1 -- the side changes at the thin edge-on moments, not at full width
  local lift = 45 * math.sin(math.min(1, progress / .75) * math.pi)
  local word = D.L(heads and "HEADS" or "TAILS")
  love.graphics.push()
  love.graphics.translate(770, 385 - lift)
  love.graphics.scale(squash, 1)
  color(C.white)
  local image = ui.coin_images[ui.flip_animation.id]
  love.graphics.draw(image, -150, -150, 0, 300 / image:getWidth(), 300 / image:getHeight())
  color(C.ink)
  love.graphics.rectangle("fill", -90, 76, 180, 44, 6)
  if not (edge and progress > .85) then text(word, -ui.f32:getWidth(word) / 2, 82, ui.f32, heads and C.blue or C.red) end
  love.graphics.pop()
  if edge and progress > .85 then centered("EDGE", 640, 460, 260, ui.f32, C.purple) end
end

local function draw_encounter()
  local e = ui.game.encounter
  local heads_bet = Game.side_bet_quote(ui.game, "Heads")
  local tails_bet = Game.side_bet_quote(ui.game, "Tails")
  local bet_available = heads_bet and ui.game.player.gold >= heads_bet.stake
    or tails_bet and ui.game.player.gold >= tails_bet.stake
  box(0, 0, 1280, 800, C.felt_dark)
  box(36, 36, 1208, 728, C.screen)
  outline(36, 36, 1208, 728, C.gold)

  -- header: logo + level, points in the middle, menu and stats on the right (no boxes, like the shop)
  color(C.white)
  local logo_scale = 64 / ui.ui_images.logo:getHeight()
  love.graphics.draw(ui.ui_images.logo, 70, 46, 0, logo_scale, logo_scale)
  text(e.endless and D.L("LEVEL %d", ui.game.encounter_index) or D.L("LEVEL %d / 8", ui.game.encounter_index), 70, 118, ui.f16, C.muted)
  text(e.endless and D.L("ENDLESS %d", e.endless) or e.boss and D.L("THE HOUSE") or D.L(e.name):upper(), 70, 136, ui.f20, e.boss and C.red or C.face)
  if e.contract then
    local def = Game.contract_def(e.contract.id)
    local progress = def and e.contract.id == "quick_clear" and D.L("FLIPS %d / 6", e.flips)
      or def and e.contract.id == "clean_run" and D.L("DISCARDS %d", e.discards)
      or def and e.contract.id == "hot_streak" and D.L("BEST COMBO %d / 4", e.best_combo_len or 0)
      or def and e.contract.id == "bank_once" and D.L(e.combo_banked and "BANKED" or "NOT BANKED")
      or def and e.contract.id == "amazon_prime" and D.L("COINS LEFT %d / 3", Game.coins_left(ui.game))
    local contract_text = e.contract.result == "COMPLETE" and D.L("CONTRACT COMPLETE")
      or e.contract.result == "MISSED" and D.L("CONTRACT MISSED")
      or def and D.L("CONTRACT: %s", D.L(def.name)) .. "  " .. progress .. "  " ..
        D.L(def.drawback)
    if contract_text then centered(contract_text, 330, 152, 880, ui.f16, e.contract.result == "COMPLETE" and C.green or C.gold) end
  end
  local met = e.quota <= 0
  local every = Game.rule(ui.game, "boss_every", 5)
  local caption, caption_color = "POINTS", C.muted
  if e.cleared then caption, caption_color = "QUOTA MET  -  EXTRA POINTS PAY GOLD", C.green
  elseif e.boss then caption, caption_color = D.L("THE HOUSE  -  EVERY %dTH FLIP IS INVERTED", every), C.red
  elseif e.endless then caption, caption_color = D.L("INVERTED  -  EVERY %dTH FLIP", every), C.red end
  centered(caption, 330, 48, 580, ui.f16, caption_color)
  centered(e.scored .. " / " .. e.max_quota, 330, 68, 580, ui.f48, met and C.green or C.gold)
  color(C.slot_dk)
  love.graphics.rectangle("fill", 330, 128, 580, 20, 4)
  color(met and C.green or C.gold)
  local progress_width = 580 * math.min(1, e.scored / e.max_quota)
  if progress_width > 0 then love.graphics.rectangle("fill", 330, 128, progress_width, 20, 4) end
  outline(330, 128, 580, 20, C.line, 4)
  button("MENU", 1120, 56, 100, 34, C.panel_light, A.open_menu, nil, "START")
  -- quota met and nothing left to flip: the big bottom button opens the shop (where a player clicks next); an exchange,
  -- which costs gold, sits up in the corner instead
  local level_done = e.cleared and not ui.game.dealt and not ui.game.mulligan and not ui.game.pending
    and not ui.flip_animation and not ui.holding
  if level_done then
    if Game.can_exchange(ui.game) then
      D.icon_button(D.L("EXCHANGE %dG", Game.exchange_cost(ui.game)), ui.ui_images.exchange, 930, 54, 170, 38, C.panel_light, A.exchange)
    end
  elseif e.cleared then
    D.icon_button("OPEN SHOP", ui.ui_images.open_shop, 930, 54, 170, 38, C.green, A.open_shop,
      not ui.flip_animation and not ui.game.pending and not ui.game.mulligan)
  end
  local stats = {
    {"coins_left", tostring(Game.coins_left(ui.game)), Game.coins_left(ui.game) <= 2 and C.red or C.face},
    {"gold", tostring(ui.game.player.gold), C.gold},
    {"energy", tostring(ui.game.player.energy), C.blue},
  }
  local x = 1226 -- laid out from the right edge, so long numbers push the others left instead of overlapping
  for i = #stats, 1, -1 do
    local stat = stats[i]
    x = x - 36 - ui.f32:getWidth(stat[2])
    D.image_at(ui.ui_images[stat[1]], x, 106, 30)
    text(stat[2], x + 36, 106, ui.f32, stat[3])
    x = x - 22
  end

  -- left: the full bank. Coins can be selected in any order; energy is paid on the flip.
  local remaining = bank_coins()
  box(70, 170, 240, 480, C.panel_dk)
  outline(70, 170, 240, 480, C.line)
  text(D.L("COIN BANK"), 84, 184, ui.f20, C.gold)
  local discard_mode_available = (e.bank_discards or 0) > 0 and ui.game.dealt and not ui.game.pending
    and not ui.flip_animation and not ui.holding
  if not discard_mode_available then ui.bank_discard_mode = false end
  local picking = discard_mode_available and ui.bank_discard_mode
  local bank_selectable = not picking and ui.game.dealt and not ui.game.pending
    and not ui.flip_animation and not ui.holding
  if discard_mode_available then
    button(D.L(picking and "DISCARD MODE: ON" or "DISCARD MODE: OFF"), 84, 205, 214, 24,
      picking and C.orange or C.panel_light, A.toggle_bank_discard_mode, true)
  elseif bank_selectable then
    text(D.L("CLICK A COIN TO PLAY IT"), 84, 208, ui.f16, C.muted)
  end
  if picking then text(D.L("CLICK A COIN TO DISCARD IT"), 84, 630, ui.f16, C.orange) end
  local row_start = discard_mode_available and 236 or 226
  for i = 1, Game.DECK_MAX do
    local x, y = 83, row_start + (i - 1) * (discard_mode_available and 34 or 36)
    local owned = remaining[i] -- flipped and discarded coins drop off the list
    local current = owned and ui.game.dealt and owned.uid == ui.game.dealt.uid
    local discardable = owned and i <= Game.VISIBLE
    box(x, y, 214, 32, owned and C.card or C.slot_dk)
    outline(x, y, 214, 32, picking and discardable and C.orange or current and C.gold or owned and C.line or C.ink)
    if owned then
      if picking and discardable then
        ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = 214, h = 32, action = function() A.discard_bank(owned.uid) end}
      elseif bank_selectable and not current then
        ui.buttons[#ui.buttons + 1] = {x = x, y = y, w = 214, h = 32, action = function() A.coin_action(owned) end}
      end
      coin_hover(owned.id, x, y, 214, 32, Game.probability(ui.game, owned), nil, owned.upgrade)
      coin_image(owned.id, x + 3, y + 2, 28)
      local clip_x, clip_y, clip_w, clip_h = love.graphics.getScissor()
      love.graphics.setScissor(x + 36, y, 84, 32)
      local name_color = picking and not discardable and C.muted or current and C.gold or C.face
      text(catalog[owned.id].name:upper(), x + 39, y + 8, ui.f16, name_color)
      if clip_x then love.graphics.setScissor(clip_x, clip_y, clip_w, clip_h) else love.graphics.setScissor() end
      text(D.L("%d%% H", math.floor(Game.probability(ui.game, owned) * 100 + .5)), x + 124, y + 8, ui.f16, C.gold)
      local cost = catalog[owned.id].energy_cost or 0
      if cost > 0 then text("E" .. cost, x + 180, y + 8, ui.f16, C.orange) end
    else
      if i <= #ui.game.coins then centered(D.L("EMPTY"), x, y + 7, 214, ui.f16, C.muted) end
    end
  end
  text(D.L("BANK %d   OUT %d   DECK %d/%d", Game.coins_left(ui.game), e.discards, #ui.game.coins, ui.game.slots),
    84, 590, ui.f16, C.muted)

  -- active buffs ("next N coins ...") so they are never invisible
  for i, buff in ipairs(e.buffs) do
    local label
    if buff.kind == "mult" then label = D.L("BUFF x%d  (%d LEFT)", buff.amount, buff.left)
    elseif buff.kind == "odds" then label = D.L("BUFF +%d%% HEADS  (%d LEFT)", math.floor(buff.amount * 100 + .5), buff.left)
    elseif buff.kind == "swap" then label = D.L("BUFF: NEXT COIN SWAPS SIDES")
    elseif buff.kind == "heads" then label = D.L("BUFF: NEXT COIN LANDS HEADS")
    else label = D.L("BUFF %s  (%d LEFT)", D.L(buff.kind:upper()), buff.left) end
    if i <= 3 then text(label, 84, 612 + (i - 1) * 18, ui.f16, C.orange) end
  end
  if #e.buffs > 3 then text(D.L("+%d MORE BUFFS", #e.buffs - 3), 84, 666, ui.f16, C.orange) end

  -- centre: the stage. One big coin, its two effects either side, the odds under it.
  local SX = 770
  box(330, 170, 880, 480, C.card)
  outline(330, 170, 880, 480, C.gold)
  local result = not ui.flip_animation and (ui.game.pending or ui.holding and ui.game.last_result or ui.game.dealt or ui.game.last_result)
  local item = result and Game.get_coin(ui.game, result.uid)
  local outcome = result and (result.final or result.result)
  local stage_title = ui.flip_animation and "FLIPPING" or ui.game.pending and "CURRENT FLIP" or
    ui.game.dealt and not ui.holding and "SELECTED COIN" or item and "LAST FLIP" or "NO COIN"
  centered(D.L(stage_title), 330, 186, 880, ui.f20, C.gold)
  if result and result.altered and result.raw and not ui.flip_animation then
    centered(D.L("ROLLED %s  >  %s  (%s)", D.L(result.raw):upper(), D.L(outcome):upper(), D.L(result.altered)),
      330, 214, 880, ui.f16, C.orange)
  end
  color(C.panel_dk)
  love.graphics.circle("fill", SX, 385, 160)
  if item then
    coin_face(SX, 385, 135, nil, true, item.id)
  elseif not ui.flip_animation then
    coin_image("back", SX - 150, 235, 300)
  end
  draw_flip_animation()
  if ui.game.dealt and not ui.game.pending and not ui.flip_animation and not ui.holding
      and Game.can_flip(ui.game) then
    ui.buttons[#ui.buttons + 1] = {
      x = SX - 135, y = 250, w = 270, h = 270,
      action = A.next_or_flip,
    }
  end

  -- combo meter: consecutive identical results multiply points; drawn in the stage's top right corner
  do
    local len, side = e.combo_len, e.combo_side
    local mult = math.min(e.combo_cap, 1 + e.combo_step * (math.max(len, 1) - 1))
    local tint = len < 2 and C.muted or side == "Heads" and C.blue or C.red
    local label = len >= 1 and D.L("COMBO  %s x%d", D.L(side == "Heads" and "HEADS" or "TAILS"), len) or D.L("COMBO")
    local face = ui.f16
    text(label, 1190 - face:getWidth(D.L(label)), 184, face, tint)
    local big = string.format("x%.2f", mult)
    text(big, 1190 - ui.f32:getWidth(big), 202, ui.f32, len < 2 and C.muted or C.gold)
    if e.shield > 0 then text(D.L("SHIELD %d", e.shield), 940, 238, face, C.green) end
    if (e.combo_pot or 0) > 0 then
      if ui.holding and Game.can_bank_combo(ui.game) then
        button(D.L("BANK %dG", e.combo_pot), 1040, 232, 150, 34, C.green, A.bank_combo, true)
      else
        local pot = D.L("POT %dG", e.combo_pot)
        text(pot, 1190 - face:getWidth(pot), 238, face, C.gold)
      end
      text(D.L("BREAK LOSES POT"), 1000, 304, ui.f16, C.red)
    end
    if e.side_bet then
      local bet = e.side_bet
      local label = bet.outcome == "WON" and D.L("BET WON  +%dG", bet.payout)
        or bet.outcome == "LOST" and D.L("BET LOST  -%dG", bet.stake)
        or bet.outcome == "PUSH" and D.L("BET PUSHED") or D.L("BET %s  %dG", D.L(bet.side:upper()), bet.stake)
      text(label, 940, 270, face, bet.outcome == "WON" and C.green or bet.outcome == "LOST" and C.red or C.gold)
    end
  end

  -- this level's modifier (from level 2 on)
  if e.modifier then
    local m = ui.modifier_catalog[e.modifier]
    text("MODIFIER", 346, 556, ui.f16, C.muted)
    text(m.name:upper(), 346, 576, ui.f20, C.orange)
    love.graphics.setFont(ui.f16)
    color(C.muted)
    love.graphics.printf(D.L(m.description), 346, 602, 250)
  end

  -- the two effects
  local coin = item and catalog[item.id] or ui.flip_animation and catalog[ui.flip_animation.id]
  for k, side in ipairs({"HEADS", "TAILS"}) do
    local cx = k == 1 and 354 or 986
    local accent = k == 1 and C.blue or C.red
    box(cx, 320, 200, 130, coin and C.panel_dk or C.slot_dk)
    outline(cx, 320, 200, 130, coin and accent or C.line)
    text(side, cx + 14, 332, ui.f20, accent)
    love.graphics.setFont(ui.f16)
    color(coin and C.face or C.muted)
    love.graphics.printf(coin and effect_description(k == 1 and coin.heads or coin.tails) or "?", cx + 14, 368, 172)
  end

  -- result banner on the coin once it has landed
  if outcome and not ui.flip_animation then
    local note, note_color = "", C.muted
    if ui.game.dealt and not ui.holding then note = "FLIP OR SELECT ANOTHER COIN"
    elseif ui.game.pending then note = "APPLYING..."
    elseif result.gained and result.gained > 0 and result.penalty and result.penalty > 0 then
      note, note_color = D.L("+%d PTS / QUOTA +%d", result.gained, result.penalty), C.purple
    elseif result.gained and result.gained > 0 then note, note_color = D.L("+%d POINTS", result.gained), C.green
      if result.combo and result.combo.mult > 1.001 then note = D.L("+%d POINTS  (x%.2f)", result.gained, result.combo.mult) end
    elseif result.penalty and result.penalty > 0 then note, note_color = D.L("QUOTA +%d", result.penalty), C.red
    elseif result.gained then note = "NO POINTS" end
    if not (ui.game.dealt and not ui.holding) then
      local accent = outcome == "Heads" and C.blue or outcome == "Tie" and C.purple or C.red
      box(SX - 130, 456, 260, 62, C.ink)
      outline(SX - 130, 456, 260, 62, accent)
      centered(outcome == "Tie" and "EDGE" or outcome:upper(), SX - 130, 460, 260, ui.f32, accent)
      centered(note, SX - 130, 496, 260, ui.f16, note_color)
    end
  end

  -- name, odds bar
  local shown_cost = coin and coin.energy_cost or 0
  centered(coin and coin.name:upper() or ui.flip_animation and "DRAWING..." or "NO COINS LEFT", 330, 548, 880, ui.f32, C.face)
  if coin and result then
    local chance = result.probability
    local tie = result.tie_probability or 0
    color(C.blue)
    love.graphics.rectangle("fill", SX - 170, 596, 340 * chance, 10)
    if tie > 0 then
      color(C.purple)
      love.graphics.rectangle("fill", SX - 170 + 340 * chance, 596, 340 * tie, 10)
    end
    color(C.red)
    love.graphics.rectangle("fill", SX - 170 + 340 * (chance + tie), 596, 340 * (1 - chance - tie), 10)
    text(D.L("HEADS %d%%", math.floor(chance * 100 + .5)), SX - 170, 612, ui.f16, C.blue)
    if tie > 0 then centered(D.L("EDGE %d%%", math.floor(tie * 100 + .5)), SX - 55, 612, 110, ui.f16, C.purple) end
    local tails_text = D.L("TAILS %d%%", math.floor((1 - chance - tie) * 100 + .5))
    text(tails_text, SX + 170 - ui.f16:getWidth(tails_text), 612, ui.f16, C.red)
    if shown_cost > 0 and not ui.flip_animation and not (e.flips == 0 and not e.side_bet and bet_available) then
      centered(D.L("ENERGY COST %d", shown_cost), SX - 60, tie > 0 and 632 or 612, 120, ui.f16, C.orange)
    end
  elseif not ui.flip_animation then
    centered("ONE COIN AT A TIME", 330, 596, 880, ui.f16, C.muted)
  end

  -- bottom: hint on the left, buttons, then chips; no bar behind them
  local hint
  if e.cleared then hint = "Keep going for gold, or open the shop." end
  if not ui.game.dealt and not ui.game.pending and not ui.flip_animation and not ui.holding then
    hint = not Game.can_exchange(ui.game) and "No coins left." or nil
  end
  if ui.game.dealt and not ui.holding and Game.flip_cost(ui.game, ui.game.dealt.uid) > ui.game.player.energy
      and Game.coins_left(ui.game) <= 1 then
    hint = D.L("LAST COIN EMERGENCY FEE: %dG", math.min(2, ui.game.player.gold))
  elseif ui.game.dealt and not ui.holding and not Game.can_flip(ui.game) then
    hint = D.L("TOO LITTLE ENERGY: SELECT ANOTHER COIN")
  end
  if ui.game.peek then
    local names = {}
    for i, uid in ipairs(ui.game.peek) do names[i] = catalog[Game.get_coin(ui.game, uid).id].name:upper() end
    hint = D.L("NEXT: %s", table.concat(names, ", "))
  end
  local mx, my = ui.mouse()
  local usable = Items.can_use(ui.game) and not ui.flip_animation and not ui.holding
  local held_items = {}
  for slot = 1, Items.MAX do
    local id = ui.game.items[slot]
    if id then held_items[#held_items + 1] = {slot = slot, id = id} end
  end
  local action_x = 640
  centered(D.L("CHIPS"), 330, 654, 280, ui.f16, C.gold)
  if #held_items > 0 then
    local chip_width = #held_items * 56 + (#held_items - 1) * 12
    local chips_x = 330 + (280 - chip_width) / 2
    for index, item in ipairs(held_items) do
      local slot = item.slot
      local x, y = chips_x + (index - 1) * 68, 698
      local def = ui.item_catalog[item.id]
      local hover = usable and mx >= x and mx <= x + 56 and my >= y and my <= y + 56
      box(x, y + (hover and -3 or 0), 56, 56, usable and C.card or C.slot_dk)
      outline(x, y + (hover and -3 or 0), 56, 56, usable and C.orange or C.line)
      D.image_at(ui.item_images[item.id], x + 12, y + 3 + (hover and -3 or 0), 32)
      centered(def.short, x, y + 37 + (hover and -3 or 0), 56, ui.f16, usable and C.face or C.muted)
      if usable then
        ui.buttons[#ui.buttons + 1] = {
          x = x, y = y, w = 56, h = 56,
          action = function() A.use_item(slot) end,
        }
      end
      if mx >= x and mx <= x + 56 and my >= y and my <= y + 56 then hint = def.description end
    end
  end
  if hint then
    love.graphics.setFont(ui.f16)
    color(C.muted)
    love.graphics.printf(D.L(hint), 70, 690, 240)
  end
  local augments = ui.game.augments or {}
  local prize_count = #augments + (ui.game.run_encounter_id and 1 or 0)
  centered(D.L("PRIZES"), 930, 654, 280, ui.f16, C.gold)
  if prize_count > 0 then
    local prize_width = prize_count * 40 + (prize_count - 1) * 5
    draw_run_modifiers(930 + (280 - prize_width) / 2, 706, 40)
  end
  local push_choice = ui.holding and ui.game.dealt ~= nil and Game.can_bank_combo(ui.game)
  local side_bet = ui.game.dealt and not ui.game.mulligan and not ui.game.pending and not ui.flip_animation
    and not ui.holding and e.flips == 0 and not e.side_bet and (heads_bet or tails_bet)
  local bet_x = 570
  if side_bet then
    if heads_bet then
      button(D.L("BET %s %dG  >  %dG", D.L("HEADS"), heads_bet.stake, heads_bet.payout), bet_x, 652, 190, 34, C.blue,
        function() A.side_bet("Heads") end, ui.game.player.gold >= heads_bet.stake)
    end
    if tails_bet then
      button(D.L("BET %s %dG  >  %dG", D.L("TAILS"), tails_bet.stake, tails_bet.payout), bet_x + 210, 652, 190, 34, C.red,
        function() A.side_bet("Tails") end, ui.game.player.gold >= tails_bet.stake)
    end
  end
  local empty_stack = not ui.game.dealt and not ui.game.pending
    and not ui.flip_animation and not ui.holding
  local out_of_coins = empty_stack and not e.cleared and Game.can_exchange(ui.game)
  if out_of_coins then
    -- the run is about to end: a notice over the stage with the three ways on
    color(C.ink, .72)
    love.graphics.rectangle("fill", 330, 170, 880, 480, 6)
    box(500, 230, 540, 380, C.panel_dk)
    outline(500, 230, 540, 380, C.red)
    centered("OUT OF COINS", 500, 246, 540, ui.f32, C.red)
    centered(D.L("%d POINTS SHORT OF THE QUOTA", e.quota) .. "  -  " .. D.L("EXCHANGES LEFT: %d", Game.exchanges_left(ui.game)),
      500, 290, 540, ui.f16, C.muted)
    local offset = 0
    if Game.can_bank_combo(ui.game) then
      D.button(D.L("BANK %dG", e.combo_pot), 530, 326, 480, 48, C.green, A.bank_combo)
      offset = 58
    end
    D.icon_button(D.L("BUY MORE COINS  %d GOLD > %d", Game.exchange_cost(ui.game), Game.EXCHANGE_GAIN),
      ui.ui_images.exchange, 530, 356 + offset, 480, 56, C.green, A.exchange)
    D.icon_button("START AGAIN", ui.ui_images.start_level, 530, 424 + offset, 480, 56, C.gold, function() A.start() end)
    D.icon_button("BACK TO MENU", ui.ui_images.give_up, 530, 492 + offset, 480, 56, C.panel_light, A.open_menu)
  elseif empty_stack then
    -- cleared with an empty stack: the shop is the main button, the exchange is the small one in the corner
    if e.cleared then
      D.icon_button("OPEN SHOP", ui.ui_images.open_shop, 640, 698, 260, 56, C.green, A.open_shop, nil, "X")
    end
  elseif push_choice then
    local can_push = Game.can_flip(ui.game)
    D.icon_button(can_push and "PUSH" or D.L("NEED %d ENERGY", Game.flip_cost(ui.game, ui.game.dealt.uid)),
      ui.ui_images.flip, 640, 698, 260, 56, C.blue, A.push_combo, can_push, "X")
  else
    -- after the last coin there is nothing left to deal, but the result is still shown: the button must still work
    local flip_label = ui.flip_animation and "FLIPPING..." or ui.holding and (ui.game.dealt and "NEXT COIN" or "CONTINUE") or "FLIP"
    local can_act = not ui.flip_animation and not ui.game.pending and (ui.holding or ui.game.dealt ~= nil)
    if can_act and not ui.holding and not Game.can_flip(ui.game) then
      flip_label = D.L("NEED %d ENERGY", Game.flip_cost(ui.game, ui.game.dealt.uid))
      can_act = false
    end
    D.icon_button(flip_label, ui.holding and ui.ui_images.next_coin or ui.ui_images.flip, 640, 698, 260, 56, C.blue,
      A.next_or_flip, can_act, "X")
  end
end

return draw_encounter
