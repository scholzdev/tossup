local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, box, outline, centered, button = D.C, D.box, D.outline, D.centered, D.button

local function draw_pending(g)
  local pending = g.augment_pending
  local def = Game.augment_def(pending.id)
  local headings = {
    epic_windfall = "CHOOSE A COIN TO REPLACE",
    reforger = "CHOOSE A COIN TO REFORGE",
    type_specialist = "CHOOSE A COIN TYPE",
    upgrade_press = "CHOOSE A COIN UPGRADE",
  }
  centered(D.L(headings[pending.id] or def.name:upper()), 70, 126, 1140, ui.f20, C.gold)
  local subtitle = pending.reward_id and D.L("NEW COIN: %s", D.L(ui.catalog[pending.reward_id].name)) or D.L(def.description)
  centered(subtitle, 70, 158, 1140, ui.f16, C.muted)
  if pending.reward_id then
    D.coin_image(pending.reward_id, 1154, 140, 42)
    D.coin_hover(pending.reward_id, 1154, 140, 42, 42)
  end

  local choices = Game.augment_choices(g)
  local cols = #choices > 12 and 4 or #choices > 6 and 3 or 2
  local rows = math.ceil(#choices / cols)
  local width, gap = math.floor((1160 - (cols - 1) * 16) / cols), 16
  local height = math.min(116, math.floor((510 - (rows - 1) * gap) / rows))
  local start_x = (1280 - (width * cols + gap * (cols - 1))) / 2
  for i, choice in ipairs(choices) do
    local x = start_x + ((i - 1) % cols) * (width + gap)
    local y = 210 + math.floor((i - 1) / cols) * (height + gap)
    box(x, y, width, height, C.panel_dk)
    outline(x, y, width, height, C.line)
    if choice.coin_id then
      D.coin_image(choice.coin_id, x + 12, y + 10, 44)
      D.coin_hover(choice.coin_id, x, y, width, height, nil, nil, choice.current_upgrade)
    end
    local text_x = x + (choice.coin_id and 64 or 16)
    love.graphics.setFont(ui.f20)
    D.color(C.gold)
    local title = choice.coin_id and ui.catalog[choice.coin_id].name or D.L(choice.title)
    love.graphics.printf(title, text_x, y + 8, width - (text_x - x) - 12, "left")
    love.graphics.setFont(ui.f16)
    D.color(C.face)
    local compact = #choices > 12
    local detail = choice.upgrade_name and D.L(choice.upgrade_name)
      .. (compact and "" or ": " .. D.L(choice.detail)) or D.L(choice.detail)
    love.graphics.printf(detail, text_x, y + 35, width - (text_x - x) - 12, "left")
    if compact and choice.upgrade_name then D.text_hover(D.L(choice.upgrade_name), D.L(choice.detail), x, y, width, height) end
    button(D.L("CHOOSE"), x + 12, y + height - 35, width - 24, 28, C.blue,
      function() A.choose_augment_option(choice.key) end, true)
  end
end

local function draw_augments()
  local g = ui.game
  box(0, 0, 1280, 800, C.felt_dark)
  box(36, 36, 1208, 728, C.screen)
  outline(36, 36, 1208, 728, C.gold)

  centered(D.L("LEVEL %d AUGMENT", g.augment_level), 70, 82, 1140, ui.f32, C.gold)
  if g.augment_pending then draw_pending(g) return end
  centered(D.L("Choose a run upgrade or change a coin before this level."),
    70, 128, 1140, ui.f20, C.muted)

  local width, height, gap = 300, 390, 30
  local start_x = (1280 - (width * 3 + gap * 2)) / 2
  for i, id in ipairs(g.augment_options or {}) do
    local def = Game.augment_def(id)
    if def then
      local x, y = start_x + (i - 1) * (width + gap), 205
      box(x, y, width, height, C.panel_dk)
      outline(x, y, width, height, C.line)
      centered(D.L(def.name), x + 16, y + 26, width - 32, ui.f20, C.gold)
      local image = ui.augment_images[id]
      if image then D.image_at(image, x + (width - 96) / 2, y + 70, 96) end
      centered(D.L(def.tier:upper() .. " TIER"), x + 16, y + 174, width - 32, ui.f16, C.muted)
      love.graphics.setFont(ui.f16)
      D.color(C.face)
      love.graphics.printf(D.L(def.description), x + 28, y + 212, width - 56, "center")
      button(D.L("CHOOSE AUGMENT"), x + 24, y + 326, width - 48, 48, C.blue,
        function() A.choose_augment(id) end, true)
    end
  end
end

return draw_augments
