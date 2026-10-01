-- End of run: full-screen like the shop, with the outcome, what you reached and a way back in.
local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_face, coin_hover = D.coin_face, D.coin_hover
local characters = ui.characters

local function draw_end()
  local g = ui.game
  local won = g.phase == "VICTORY"
  box(0, 0, 1280, 800, C.felt_dark)
  box(36, 36, 1208, 728, C.screen)
  outline(36, 36, 1208, 728, C.gold)

  -- headline, drawn at double size in the outcome colour with a shadow
  local headline = D.L(g.endless and "ENDLESS RUN OVER" or won and "THE HOUSE FALLS" or "RUN OVER")
  love.graphics.setFont(ui.f48)
  local width = ui.f48:getWidth(headline) * 2
  color(C.black, .35)
  love.graphics.print(headline, math.floor(640 - width / 2) + 4, 90 + 4, 0, 2, 2)
  color(won and C.green or C.red)
  love.graphics.print(headline, math.floor(640 - width / 2), 90, 0, 2, 2)

  -- the character and their starting coin
  local def = characters[g.character_id]
  box(380, 250, 230, 300, C.panel_dk)
  outline(380, 250, 230, 300, C.line)
  local portrait = ui.character_images[g.character_id]
  local scale = math.min(210 / portrait:getWidth(), 240 / portrait:getHeight())
  color(C.white)
  love.graphics.draw(portrait, 380 + (230 - portrait:getWidth() * scale) / 2, 258 + (250 - portrait:getHeight() * scale) / 2,
    0, scale, scale)
  centered(def.name:upper(), 380, 520, 230, ui.f20, C.gold)

  -- what the run reached
  box(650, 250, 250, 300, C.panel_dk)
  outline(650, 250, 250, 300, C.line)
  centered(g.endless and "ENDLESS MODE" or won and "YOU WON THE RUN" or "TRY A NEW SET", 650, 266, 250, ui.f20, C.face)
  centered(tostring(g.endless and g.cleared - 4 or g.cleared), 650, 320, 250, ui.f48, won and C.green or C.gold)
  centered(g.endless and "ENDLESS LEVELS CLEARED" or "LEVELS CLEARED OF 4", 650, 380, 250, ui.f16, C.muted)
  D.image_at(ui.ui_images.gold, 690, 430, 44)
  text(tostring(g.player.gold), 746, 436, ui.f32, C.gold)
  text("GOLD LEFT", 690, 484, ui.f16, C.muted)
  text(D.L("SEED %s", g.seed), 690, 512, ui.f16, C.muted)
  if not won and g.lost_why then
    love.graphics.setFont(ui.f16)
    color(C.red)
    love.graphics.printf((function(s) return s:sub(1, 1):upper() .. s:sub(2) end)(D.L(g.lost_why)), 380, 570, 520, "center")
  end

  if won and not g.endless then
    -- the boss fell: keep going through endless levels, or start over
    D.icon_button("ENDLESS MODE", ui.ui_images.next_coin, 470, 584, 340, 60, C.gold, A.continue_endless)
    D.icon_button("NEW RUN", ui.ui_images.start_level, 470, 652, 340, 56, C.green, function() A.start() end)
    button("BACK TO MENU", 520, 718, 240, 36, C.panel_light, A.open_menu)
  else
    D.icon_button("NEW RUN", ui.ui_images.start_level, 470, 600, 340, 64, C.green, function() A.start() end)
    button("BACK TO MENU", 520, 686, 240, 44, C.panel_light, A.open_menu)
  end
  button("MENU", 1120, 56, 100, 34, C.panel_light, A.open_menu)
end

return draw_end
