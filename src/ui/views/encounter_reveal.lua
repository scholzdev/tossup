local ui = require("src.ui.state")
local D = require("src.ui.draw")
local Game = require("src.game")
local C, color, box, outline, centered = D.C, D.color, D.box, D.outline, D.centered

local function clamp(value, low, high)
  return math.max(low, math.min(high, value))
end

local function draw_encounter_reveal()
  local reveal = ui.encounter_reveal
  local game = ui.game
  if not reveal or not game then return end

  local encounter = Game.encounter_def(game.run_encounter_id)
  if not encounter then return end
  local elapsed = reveal.elapsed
  local fade_in = clamp(elapsed / .32, 0, 1)
  local alpha = fade_in
  local rise = 1 - (1 - fade_in) ^ 3
  local pulse = 1 + math.sin(elapsed * 5.5) * .025
  local center_x, center_y = 640, 360

  color(C.ink, .88 * alpha)
  love.graphics.rectangle("fill", 0, 0, 1280, 800)

  -- Moving rays and expanding rings give the run-wide Encounter a single theatrical reveal.
  love.graphics.push()
  love.graphics.translate(center_x, center_y - 22)
  color(C.gold, .24 * alpha)
  love.graphics.setLineWidth(3)
  for i = 0, 19 do
    local angle = i * math.pi / 10 + elapsed * .24
    local inner = 142 + math.sin(elapsed * 3 + i) * 8
    local outer = 226 + math.sin(elapsed * 2.2 + i * .7) * 12
    love.graphics.line(math.cos(angle) * inner, math.sin(angle) * inner,
      math.cos(angle) * outer, math.sin(angle) * outer)
  end
  for i = 1, 3 do
    love.graphics.circle("line", 0, 0, 185 + i * 24 + math.sin(elapsed * 3 - i) * 7)
  end
  love.graphics.pop()

  local panel_y = 95 + (1 - rise) * 84
  box(255, panel_y, 770, 545, C.panel_dk)
  outline(255, panel_y, 770, 545, C.gold, 5)
  outline(270, panel_y + 15, 740, 515, C.line, 2)

  centered(D.L("RUN ENCOUNTER"), 295, panel_y + 34, 690, ui.f20, C.gold)
  centered(D.L("ONE RULE FOR THE WHOLE RUN"), 295, panel_y + 74, 690, ui.f16, C.muted)

  local image = ui.encounter_images[game.run_encounter_id]
  if not image then return end
  local icon_scale = 190 / image:getWidth() * pulse * (.72 + .28 * rise)
  love.graphics.push()
  love.graphics.translate(center_x, center_y - 34 + (1 - rise) * 40)
  love.graphics.rotate(math.sin(elapsed * 1.8) * .09)
  love.graphics.setColor(1, 1, 1, alpha)
  love.graphics.draw(image, 0, 0, 0, icon_scale, icon_scale, image:getWidth() / 2, image:getHeight() / 2)
  love.graphics.pop()

  centered(D.L(encounter.name):upper(), 295, panel_y + 374, 690, ui.f32, C.face)
  love.graphics.setFont(ui.f16)
  color(C.muted, alpha)
  love.graphics.printf(D.L(encounter.description), 365, panel_y + 420, 550, "center")

  if elapsed > .65 then
    centered(D.L("CLICK OR PRESS ANY KEY TO CONTINUE"), 300, 690, 680, ui.f16,
      C.muted)
  end
end

return draw_encounter_reveal
