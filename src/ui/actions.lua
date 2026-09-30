-- Player actions and per-frame flow (flip animation -> hold -> resolve).
local Game = require("src.game")
local Profile = require("src.profile")
local ui = require("src.ui.state")

local A = {}

local PROFILE_FILE = "profile.lua"

function A.load_profile()
  ui.profile = Profile.decode(love.filesystem.getInfo(PROFILE_FILE) and love.filesystem.read(PROFILE_FILE))
end

local function save_profile()
  love.filesystem.write(PROFILE_FILE, Profile.encode(ui.profile))
end

-- Spend tokens to unlock a locked coin for the selected character.
function A.unlock_coin(coin_id)
  if Profile.unlock(ui.profile, ui.selected_character, coin_id) then
    save_profile()
    return true
  end
  return false
end

function A.start(seed)
  ui.game = Game.new(seed or (os.time() + math.floor(love.timer.getTime() * 1000000)),
    ui.selected_character, Profile.unlocked_list(ui.profile, ui.selected_character))
  ui.flip_animation = nil
  ui.resolve_timer = 0
  ui.holding = false
  ui.notice = ""
end

function A.select_character(id)
  ui.selected_character = id
  ui.coin_page = 1
end

-- Every coin the menu shows for a character: base pool first, then the locked ones.
function A.menu_coins(character_id)
  local def = ui.characters[character_id]
  local list = {}
  for _, id in ipairs(def.pool) do list[#list + 1] = {id = id} end
  for _, entry in ipairs(def.locked or {}) do
    list[#list + 1] = {id = entry[1], cost = entry[2],
      locked = not Profile.is_unlocked(ui.profile, character_id, entry[1])}
  end
  return list
end

function A.change_coin_page(delta)
  local pages = math.ceil(#A.menu_coins(ui.selected_character) / ui.coins_per_page)
  ui.coin_page = math.max(1, math.min(pages, ui.coin_page + delta))
end

function A.coin_action(item)
  ui.notice = ""
  Game.select(ui.game, item.uid)
end

function A.use_item(slot)
  if not ui.flip_animation then Game.use_item(ui.game, slot) end
end

function A.flip_next_coin()
  local game = ui.game
  if not Game.flip(game) then return end
  ui.flip_animation = {id = Game.get_coin(game, game.pending.uid).id,
    outcome = game.pending.result, elapsed = 0, duration = 1.6}
end

function A.next_or_flip()
  if ui.holding then
    if ui.game.pending or ui.flip_animation then return end
    ui.holding = false
  else
    A.flip_next_coin()
  end
end

function A.update(dt)
  local game = ui.game
  ui.shake = math.max(0, ui.shake - dt)
  if game and game.phase ~= "ENCOUNTER" then ui.holding = false end
  if game and (game.phase == "GAME_OVER" or game.phase == "VICTORY") and not game.tokens_paid then
    game.tokens_paid = Game.run_tokens(game)
    ui.profile.tokens = ui.profile.tokens + game.tokens_paid
    save_profile()
  end
  if ui.resolve_timer > 0 and game and not game.paused then
    ui.resolve_timer = ui.resolve_timer - dt
    if ui.resolve_timer <= 0 then Game.resolve(game) ui.holding = true end
  end
  if ui.flip_animation and game and not game.paused then
    ui.flip_animation.elapsed = ui.flip_animation.elapsed + dt
    if ui.flip_animation.elapsed >= ui.flip_animation.duration then
      ui.flip_animation = nil
      ui.shake = .3
      ui.resolve_timer = .9
    end
  end
end

return A
