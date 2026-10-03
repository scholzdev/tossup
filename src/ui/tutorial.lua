-- Interactive tutorial, played over a real throwaway run (3 Normal coins, quota 2, the first flips forced to Heads).
-- Each step dims the screen except a spotlight rectangle and shows a card next to it. A step either waits for a click
-- ("click to continue") or waits for the player to do the thing (`wait` returns true when done); in those steps only the
-- buttons inside the spotlight work. Esc or the SKIP button leaves the tutorial. Nothing from the run is saved.
local Game = require("src.game")
local ui = require("src.ui.state")
local D = require("src.ui.draw")
local Tutorial = {}

local C, color, box, outline, text = D.C, D.color, D.box, D.outline, D.text
local L = D.L

-- rect = {x, y, w, h} in canvas coordinates (the same layout the views use)
local STEPS = {
  {title = "THE GOAL", rect = {330, 40, 580, 120},
   text = "Each level has a QUOTA: points you must score before your coins run out. The bar fills as you score."},
  {title = "YOUR RESOURCES", rect = {930, 96, 290, 46},
   text = "Coins left in your stack, your gold, and your energy. Strong coins cost energy to flip."},
  {title = "THE OPENING HAND", rect = {183, 270, 914, 480},
   text = "Every level starts with an OPENING HAND. Click coins to mark them, then Discard throws them away for free. This time keep them all and press START LEVEL.",
   wait = function(g) return not g.mulligan end, hint = "PRESS START LEVEL"},
  {title = "THE COIN BANK", rect = {70, 170, 240, 480},
   enter = function(g) -- discarding in the opening hand must not make the quota unreachable (a Normal coin scores 1)
     local e = g.encounter
     e.quota = math.min(e.quota, Game.coins_left(g))
     e.max_quota = e.quota
   end,
   text = "Your next three coins. The first one is up next. A level lasts exactly as long as your stack: every coin is played once."},
  {title = "THE COIN IN PLAY", rect = {330, 170, 880, 480},
   text = "The coin with its Heads effect (blue, left) and Tails effect (red, right), and its odds below. Read them before you flip."},
  {title = "FLIP", rect = {550, 676, 260, 64},
   text = "Press FLIP (or Space) to toss the coin.",
   wait = function(g) return g.last_result ~= nil end, hint = "PRESS FLIP"},
  {title = "THE RESULT", rect = {330, 450, 880, 300},
   text = "The banner shows the side it landed on and the points you got. Press NEXT COIN to continue.",
   wait = function(g) return g.last_result ~= nil and not ui.holding end, hint = "PRESS NEXT COIN"},
  {title = "CHIPS", rect = {830, 676, 376, 64},
   text = "These three slots hold CHIPS: one-use helpers you buy in the shop. Click one while a coin is in play to use it, for example an Energy Drink for 2 more energy. You can hold three."},
  {title = "DISCARD", rect = {330, 676, 200, 64},
   text = "Don't like the coin in play? DISCARD throws it away for the level, for free, but you lose that flip. Only the current coin can be discarded."},
  {title = "FLIP AGAIN", rect = {550, 676, 260, 64},
   text = "Flip this one too. Two Heads in a row start a COMBO.",
   wait = function(g) return g.encounter and g.encounter.cleared end, hint = "PRESS FLIP"},
  {title = "COMBO AND QUOTA", rect = {320, 650, 510, 110},
   text = "The same result raises your combo multiplier and an unbanked gold pot. After a flip, choose BANK to keep the pot and end the combo, or PUSH to risk losing it if the streak breaks. Clearing the level banks any pot left."},
  {title = "OPEN THE SHOP", rect = {930, 54, 170, 38},
   text = "Keep flipping for extra gold, or press OPEN SHOP to move on.",
   wait = function(g) return g.phase == "SHOP" end, hint = "PRESS OPEN SHOP"},
  {title = "THE SHOP", rect = {70, 160, 920, 440},
   text = "Between levels you buy COINS (larger and stronger decks raise the next quota), CHIPS (one-use helpers) and a PRIZE (lasts the run). REROLL refreshes the coins."},
  {title = "DECK TOOL", rect = {1000, 238, 220, 220},
   text = "Select a coin below to remove it from the deck for 8 gold."},
  {title = "YOUR DECK", rect = {70, 600, 920, 150},
   text = "Your coins. Click one to select it for removal. The dark slots on the right are extra deck slots: 5 gold, 2 more for each one you buy, up to ten."},
  {title = "NEXT ROUND", rect = {1000, 586, 220, 170},
   text = "Press the red button when you are ready for the next level.",
   wait = function(g) return g.phase == "ENCOUNTER" end, hint = "PRESS NEXT ROUND"},
  {title = "A NEW MODIFIER", rect = {330, 540, 300, 110},
   enter = function(g) if g.mulligan then Game.mulligan_done(g) end end,
   text = "From level 2 on, every level has a MODIFIER: a bonus or a twist for this level only. Read it here before you flip."},
  {title = "RUNNING OUT OF COINS", rect = {930, 96, 100, 46},
   text = "If your coins run out before the quota is met, pay gold to EXCHANGE: three played coins come back, only a limited number of times per level. After that the run is over."},
  {title = "THAT'S IT", rect = {330, 280, 620, 200},
   text = "Beat three levels and then The House. Build a deck that scores, keep your combos going, and spend your gold well. Win a run to unlock the next character. Good luck!"},
}

-- Start the tutorial on a fresh throwaway run.
function Tutorial.start()
  local game = Game.new(7, "blade", {}, {"normal", "normal", "normal"}, true)
  game.run_encounter_id = nil -- the scripted tutorial should not gain a random run rule
  game.encounter.payout = Game.stage(1).payout
  game.tutorial = true -- not logged, no tokens, no collection
  game.tutorial_heads = 3 -- the first flips land Heads, so the tutorial always works
  game.items = {"energy_drink"} -- so the chip slots are not empty
  game.encounter.quota, game.encounter.max_quota = 2, 2
  game.paused = false
  ui.game, ui.flip_animation, ui.holding, ui.marked, ui.resolve_timer = game, nil, false, {}, 0
  ui.tutorial = {step = 1}
end

function Tutorial.finish()
  ui.tutorial, ui.game, ui.flip_animation, ui.holding, ui.marked = nil, nil, nil, false, {}
  ui.screen = "title"
end

function Tutorial.step() return ui.tutorial and STEPS[ui.tutorial.step] end

function Tutorial.interactive() local s = Tutorial.step() return s and s.wait ~= nil end

function Tutorial.next()
  local t = ui.tutorial
  if not t then return end
  t.step = t.step + 1
  if t.step > #STEPS then Tutorial.finish() return end
  local entered = STEPS[t.step]
  if entered.enter and ui.game then entered.enter(ui.game) end -- a step may set the scene (start the next level)
end

-- Steps that wait for the player move on by themselves once the thing is done.
function Tutorial.update()
  local s = Tutorial.step()
  if s and s.wait and ui.game and s.wait(ui.game) then Tutorial.next() end
end

-- The card sits below the spotlight, or above it when there is no room.
local function card_rect(step)
  local rx, ry, rw, rh = step.rect[1], step.rect[2], step.rect[3], step.rect[4]
  local w = 480
  local _, lines = ui.f16:getWrap(L(step.text), w - 40)
  local h = 96 + #lines * 20
  local x = math.min(1280 - w - 20, math.max(20, rx + rw / 2 - w / 2))
  local y = ry + rh + 20
  if y + h > 790 then y = ry - h - 20 end
  if y < 10 then y = math.max(10, ry + 16) end -- a spotlight that fills the screen: the card goes inside it
  return x, y, w, h
end

-- Draw over the finished frame and narrow the clickable buttons to what the step allows.
function Tutorial.draw()
  local step = Tutorial.step()
  if not step then return end
  local rx, ry, rw, rh = step.rect[1], step.rect[2], step.rect[3], step.rect[4]
  color(C.ink, .72)
  love.graphics.rectangle("fill", 0, 0, 1280, ry)
  love.graphics.rectangle("fill", 0, ry + rh, 1280, 800 - ry - rh)
  love.graphics.rectangle("fill", 0, ry, rx, rh)
  love.graphics.rectangle("fill", rx + rw, ry, 1280 - rx - rw, rh)
  color(C.orange)
  love.graphics.setLineWidth(3)
  love.graphics.rectangle("line", rx - 3, ry - 3, rw + 6, rh + 6, 6)
  love.graphics.setLineWidth(1)

  local x, y, w, h = card_rect(step)
  box(x, y, w, h, C.panel_dk)
  outline(x, y, w, h, C.orange)
  text(L(step.title), x + 20, y + 14, ui.f20, C.orange)
  love.graphics.setFont(ui.f16)
  color(C.face)
  love.graphics.printf(L(step.text), x + 20, y + 46, w - 40)
  local footer = step.wait and L(step.hint) or L("CLICK ANYWHERE TO CONTINUE")
  text(footer, x + 20, y + h - 28, ui.f16, C.gold)
  local count = ui.tutorial.step .. " / " .. #STEPS
  text(count, x + w - 20 - ui.f16:getWidth(count), y + h - 28, ui.f16, C.muted)

  local kept = {}
  if step.wait then -- a "do it" step: only the buttons inside the spotlight work
    for _, b in ipairs(ui.buttons) do
      local cx, cy = b.x + b.w / 2, b.y + b.h / 2
      if cx >= rx and cx <= rx + rw and cy >= ry and cy <= ry + rh then kept[#kept + 1] = b end
    end
  else
    kept[1] = {x = 0, y = 0, w = 1280, h = 800, action = Tutorial.next}
  end
  ui.buttons = kept
  D.button("SKIP", 1110, 750, 140, 36, C.panel_light, Tutorial.finish, nil, "B")
end

return Tutorial
