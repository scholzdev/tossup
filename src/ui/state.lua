-- Shared mutable UI state. Game rules live in src/game.lua; this is presentation only.
local Game = require("src.game")

-- The game is drawn on a fixed 1280x800 canvas that is scaled and centred to fit the window.
local function layout()
  local w, h = love.graphics.getDimensions()
  local scale = math.min(w / 1280, h / 800)
  return scale, (w - 1280 * scale) / 2, (h - 800 * scale) / 2
end

local state = {
  catalog = Game.catalog(),
  item_catalog = Game.item_catalog(),
  characters = Game.characters(),
  character_order = {"blade", "seer", "trader"},
  selected_character = "blade",
  screen = "title", -- title | select | collection | options (shown when no run is active or paused)
  sets_character = "blade", -- coin set editor: which character and which of its sets is open
  sets_index = 1,
  set_draft = nil, -- unsaved edits of the open coin set
  marked = {}, -- coins marked for discarding (uid -> true), in the opening hand or the bank
  collection_page = 1,
  collection_filter = "ALL",
  collection_sort = "rarity",
  coin_order = require("content.coin_order"),
  game = nil,
  profile = nil, -- meta progression (tokens, unlocks), loaded in app.load
  buttons = {},
  debug_visible = false,
  notice = "",
  f16 = nil, f20 = nil, f32 = nil, f48 = nil,
  coin_images = {},
  item_images = {}, -- assets/items/<id>.png
  relic_images = {}, -- assets/relics/<id>.png
  ui_images = {}, -- assets/ui/<name>.png: next_round, reroll, gold, energy, coins_left
  character_images = {},
  hovered_coin = nil,
  hovered_text = nil,
  cursors = nil, -- {arrow, click} LÖVE cursors, created in app.load
  cursor_current = nil,
  flip_animation = nil,
  shake = 0,
  holding = false, -- landed coin stays in view until the player asks for the next one
  resolve_timer = 0, -- hold on the flipped result before effects apply
}

state.layout = layout

-- Window coordinates -> canvas coordinates.
function state.to_canvas(x, y)
  local scale, ox, oy = layout()
  return (x - ox) / scale, (y - oy) / scale
end

function state.mouse() return state.to_canvas(love.mouse.getPosition()) end

return state
