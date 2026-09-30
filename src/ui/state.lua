-- Shared mutable UI state. Game rules live in src/game.lua; this is presentation only.
local Game = require("src.game")

return {
  catalog = Game.catalog(),
  item_catalog = Game.item_catalog(),
  characters = Game.characters(),
  character_order = {"blade", "seer", "trader"},
  selected_character = "blade",
  coin_page = 1,
  coins_per_page = 4,
  game = nil,
  profile = nil, -- meta progression (tokens, unlocks), loaded in app.load
  buttons = {},
  debug_visible = false,
  notice = "",
  f16 = nil, f20 = nil, f32 = nil, f48 = nil,
  coin_images = {},
  character_images = {},
  hovered_coin = nil,
  flip_animation = nil,
  shake = 0,
  holding = false, -- landed coin stays in view until the player asks for the next one
  resolve_timer = 0, -- hold on the flipped result before effects apply
}
