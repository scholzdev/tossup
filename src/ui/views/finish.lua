local Game = require("src.game")
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text, centered, button = D.C, D.color, D.box, D.outline, D.text, D.centered, D.button
local coin_image, coin_face, coin_hover, effects, effect_description =
  D.coin_image, D.coin_face, D.coin_hover, D.effects, D.effect_description
local catalog, characters = ui.catalog, ui.characters

local function draw_end()
  box(292, 29, 964, 728, C.panel)
  centered(ui.game.phase == "VICTORY" and "THE HOUSE FALLS" or "RUN OVER", 320, 126, 910,
    ui.f48, ui.game.phase == "VICTORY" and C.green or C.red)
  coin_face(774, 386, 138, nil, true, characters[ui.game.character_id].starter)
  coin_hover(characters[ui.game.character_id].starter, 595, 200, 358, 358)
  centered(ui.game.phase == "VICTORY" and "YOU WON THE RUN" or "TRY A NEW DECK", 320, 561, 910,
    ui.f32, C.face)
  centered("+" .. (ui.game.tokens_paid or Game.run_tokens(ui.game)) .. " TOKENS  /  " ..
    ui.game.cleared .. " LEVELS CLEARED", 320, 603, 910, ui.f20, C.gold)
  button("NEW RUN", 626, 642, 300, 63, C.blue, function() A.start() end)
end

return draw_end
