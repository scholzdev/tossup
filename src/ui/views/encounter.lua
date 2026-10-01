local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_face, coin_hover, effects, effect_description =
  D.coin_image, D.coin_face, D.coin_hover, D.effects, D.effect_description
local Items = require("src.items")
local catalog, characters = ui.catalog, ui.characters

local function remaining_coins()
  local in_pile = {}
  for _, uid in ipairs(ui.game.encounter.pile) do in_pile[uid] = true end
  if ui.game.dealt then in_pile[ui.game.dealt.uid] = true end -- dealt but not flipped yet
  local remaining = {}
  for _, owned in ipairs(ui.game.coins) do
    if in_pile[owned.uid] then remaining[#remaining + 1] = owned end
  end
  return remaining
end

local function draw_flip_animation()
  if not ui.flip_animation then return end
  local progress = math.min(1, ui.flip_animation.elapsed / ui.flip_animation.duration)
  -- quartic ease-out so the spin slows to a tense stop; even half-turn = Tails up, odd = Heads up
  local turns = ui.flip_animation.outcome == "Heads" and 9 or 10
  local phase = turns * (1 - (1 - progress) ^ 4)
  local squash = math.max(.06, math.abs(math.cos(phase * math.pi)))
  local heads = math.floor(phase) % 2 == 1
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
  text("TOSSUP", 43, 37, ui.f32, C.gold)
  text("RUN " .. ui.game.encounter_index .. " / 4", 44, 110, ui.f16, C.muted)
  text(e.boss and "THE HOUSE" or e.name:upper(), 267, 37, ui.f32,
    e.boss and C.red or C.face)
  text(e.boss and "EVERY 5TH DRAW INVERTS THE RESULT" or
    "REACH THE QUOTA BEFORE DRAWS RUN OUT", 268, 75, ui.f16, C.muted)
  text("QUOTA  " .. e.quota .. " LEFT / " .. e.max_quota, 268, 100, ui.f16, C.gold)
  color(C.slot)
  love.graphics.rectangle("fill", 459, 105, 406, 13, 2)
  color(C.green)
  love.graphics.rectangle("fill", 459, 105, 406 * (1 - e.quota / e.max_quota), 13, 2)
  local stats = {
    {"GOLD", tostring(ui.game.player.gold), C.gold},
    {"ENERGY", tostring(ui.game.player.energy), C.blue},
  }
  for i, stat in ipairs(stats) do
    local x = 1005 + (i - 1) * 116
    box(x, 39, 106, 66, C.panel)
    text(stat[1], x + 9, 44, ui.f16, C.muted)
    text(stat[2], x + 9, 67, ui.f20, stat[3])
  end
  text("DRAWS " .. e.draws, 1006, 114, ui.f16, C.gold)
  button("MENU", 1135, 109, 102, 30, C.panel_light, A.open_menu)

  local remaining = remaining_coins()
  box(28, 160, 250, 504, C.ink)
  outline(28, 160, 250, 504, C.panel_light)
  text("COIN STACK", 42, 175, ui.f20, C.gold)
  text(#remaining .. " LEFT / " .. #ui.game.coins .. " IN DECK", 43, 207, ui.f16, C.face)
  text("RESHUFFLES WHEN EMPTY", 43, 232, ui.f16, C.muted)
  for i = 1, 5 do
    local x, y = 41, 265 + (i - 1) * 75
    local owned = remaining[i] -- flipped and discarded coins drop off the list
    local current = owned and ui.game.dealt and owned.uid == ui.game.dealt.uid
    box(x, y, 224, 64, owned and C.panel or C.slot)
    outline(x, y, 224, 64, current and C.gold or owned and C.panel_light or C.ink)
    if owned then
      coin_image(owned.id, x + 4, y + 4, 56)
      text(catalog[owned.id].name:upper(), x + 63, y + 7, ui.f20, C.face)
      text(math.floor(Game.probability(ui.game, owned) * 100 + .5) .. "% HEADS",
        x + 64, y + 36, ui.f16, C.gold)
    else
      centered("EMPTY SLOT", x, y + 20, 224, ui.f16, C.muted)
    end
  end

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
  centered(item and catalog[item.id].name:upper() or ui.flip_animation and "DRAWING..." or
    "MYSTERY COIN", 310, 578, 618, ui.f32, C.face)
  centered(item and ((outcome and outcome:upper() .. " / " or "") ..
    math.floor(result.probability * 100 + .5) .. "% HEADS") or
    "ONE COIN AT A TIME", 310, 617, 618, ui.f16,
    outcome and (outcome == "Heads" and C.blue or C.red) or C.muted)

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
    centered(ui.game.dealt and not ui.holding and "FLIP IT OR DISCARD" or ui.game.pending and "APPLYING..." or "EFFECT APPLIED", 980, 608, 252, ui.f16, C.muted)
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
  local hint = ui.holding and "Click for the next coin." or "Flip it or discard it."
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
      button(def.short, x, 692, 140, 64, C.orange, function() A.use_item(slot) end, usable)
      if mx >= x and mx <= x + 140 and my >= 692 and my <= 756 then hint = def.description end
    else
      box(x, 692, 140, 64, C.slot)
      outline(x, 692, 140, 64, C.panel_light)
      centered("ITEM", x, 714, 140, ui.f16, C.muted)
    end
  end
  text(hint, 45, 728, ui.f16, C.muted)
  if ui.game.dealt and not ui.flip_animation and not ui.holding then
    button("DISCARD / 1", 290, 692, 200, 64, C.red, function() Game.discard(ui.game) end,
      ui.game.player.energy >= 1 and #ui.game.coins - e.discards > 1)
  end
  button(ui.flip_animation and "FLIPPING..." or ui.holding and "NEXT COIN" or "FLIP", 510, 692, 260, 64, C.blue,
    A.next_or_flip, ui.game.dealt ~= nil and not ui.flip_animation and (ui.holding or not ui.game.pending))
end

return draw_encounter
