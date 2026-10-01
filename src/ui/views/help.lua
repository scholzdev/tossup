-- How to play: one screen with the whole loop. Shown once on the first Play, and from the title menu.
local ui = require("src.ui.state")
local A = require("src.ui.actions")
local D = require("src.ui.draw")
local C, color, box, outline, text = D.C, D.color, D.box, D.outline, D.text

local SECTIONS = {
  {"THE GOAL", "Score points to beat the level's QUOTA before your coins run out. There is no HP: coins score points, and bad flips raise the quota. Four levels; the last one is The House."},
  {"YOUR COINS", "You play with a small stack of coins. Each coin has a Heads chance and a Heads and a Tails effect. Every coin is played once per level; nothing is reshuffled."},
  {"EACH LEVEL", "First you see an OPENING HAND: click coins to mark them and press Discard to throw them away for free. Then the bank shows your next 3 coins. Flip the first one, or mark coins and Discard them."},
  {"ENERGY", "Strong coins cost ENERGY to flip (shown as E1, E2). You get 3 per level; Spark, Copper and Capacitor give more. A coin you cannot pay for can only be discarded."},
  {"QUOTA MET", "You are paid gold at once and the level stays open: every 2 extra points pay 1 more gold. Press OPEN SHOP (top right) when you want to move on."},
  {"OUT OF COINS", "If the quota is not met, pay gold to EXCHANGE: 3 of your played coins come back. If you cannot, the run is over."},
  {"THE SHOP", "Buy COINS (a bigger deck means a bigger quota, so buy better coins), CHIPS (one-use helpers, used mid-level), and a PRIZE (lasts the run). Odds Tuner adds Heads chance; Coin Removal drops a weak coin."},
}

local function draw_help()
  D.frame(D.title("help"), "BACK", function() A.go("title") end)
  for i, section in ipairs(SECTIONS) do
    local col, row = (i - 1) % 2, math.floor((i - 1) / 2)
    local x, y = 70 + col * 580, 160 + row * 128
    box(x, y, 560, 116, C.panel_dk)
    outline(x, y, 560, 116, C.line)
    text(section[1], x + 16, y + 10, ui.f20, C.gold)
    love.graphics.setFont(ui.f16)
    color(C.face)
    love.graphics.printf(D.L(section[2]), x + 16, y + 38, 528)
  end
  -- the 8th cell of the grid: controls
  box(650, 544, 560, 116, C.panel_dk)
  outline(650, 544, 560, 116, C.line)
  text("CONTROLS", 666, 554, ui.f20, C.gold)
  love.graphics.setFont(ui.f16)
  color(C.face)
  love.graphics.printf(D.L("Space = Flip / Next Coin.  Click = mark a coin.  Esc = menu (your run waits).  Hover any coin for details. Quitting the app loses the run."), 666, 582, 528)
  if ui.help_next then
    D.icon_button("GOT IT", ui.ui_images.start_level, 470, 684, 340, 56, C.green, function() A.go(ui.help_next) end)
  end
end

return draw_help
