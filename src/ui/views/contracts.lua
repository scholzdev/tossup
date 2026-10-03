local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, centered, button = D.C, D.color, D.box, D.outline, D.centered, D.button

local function draw_contracts()
  local g = ui.game
  local e = g.encounter
  box(0, 0, 1280, 800, C.felt_dark)
  box(36, 36, 1208, 728, C.screen)
  outline(36, 36, 1208, 728, C.gold)

  centered(D.L("LEVEL %d CONTRACT", g.encounter_index), 70, 84, 1140, ui.f32, C.gold)
  centered(D.L("Choose a challenge for a bonus, or skip it."), 70, 132, 1140, ui.f20, C.muted)
  local stage_name = e.endless and D.L("ENDLESS %d", e.endless) or D.L(e.name)
  centered(stage_name:upper(), 70, 166, 1140, ui.f16, C.face)

  local width, height, gap = 300, 360, 30
  local start_x = (1280 - (width * 3 + gap * 2)) / 2
  for i, id in ipairs(e.contract_options or {}) do
    local def = Game.contract_def(id)
    if def then
      local x, y = start_x + (i - 1) * (width + gap), 220
      box(x, y, width, height, C.panel_dk)
      outline(x, y, width, height, C.line)
      centered(D.L(def.name), x + 16, y + 34, width - 32, ui.f20, C.gold)
      local reward = def.reward_text and D.L(def.reward_text) or D.L("BONUS +%dG", def.reward)
      centered(reward, x + 16, y + 84, width - 32, def.reward_text and ui.f20 or ui.f32, C.green)
      color(C.muted)
      love.graphics.setFont(ui.f16)
      love.graphics.printf(D.L(def.description), x + 28, y + 138, width - 56, "center")
      centered(D.L("DRAWBACK"), x + 16, y + 211, width - 32, ui.f16, C.red)
      color(C.red)
      love.graphics.setFont(ui.f16)
      love.graphics.printf(D.L(def.drawback), x + 28, y + 235, width - 56, "center")
      button(D.L("TAKE CONTRACT"), x + 24, y + 296, width - 48, 52, C.blue,
        function() A.choose_contract(id) end, true)
    end
  end
  button(D.L("SKIP CONTRACT"), 500, 638, 280, 54, C.panel_light, A.skip_contract, true)
end

return draw_contracts
