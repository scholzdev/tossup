-- Player actions and per-frame flow (flip animation -> hold -> resolve).
local Game = require("src.game")
local Profile = require("src.profile")
local ui = require("src.ui.state")

local A = {}

local PROFILE_FILE = "profile.lua"

local function apply_options()
  love.window.setFullscreen(ui.profile.options.fullscreen)
end

function A.load_profile()
  ui.profile = Profile.decode(love.filesystem.getInfo(PROFILE_FILE) and love.filesystem.read(PROFILE_FILE))
  apply_options()
end

local function save_profile()
  love.filesystem.write(PROFILE_FILE, Profile.encode(ui.profile))
end

function A.go(screen)
  ui.screen = screen
  ui.collection_page = 1
end

-- Pause the run (if any) and show the title screen.
function A.open_menu()
  if ui.game then ui.game.paused = true end
  A.go("title")
end

function A.toggle_option(key)
  ui.profile.options[key] = not ui.profile.options[key]
  if key == "fullscreen" then apply_options() end
  save_profile()
end

function A.set_filter(rarity)
  ui.collection_filter = rarity
  ui.collection_page = 1
end

function A.change_collection_page(delta)
  ui.collection_page = math.max(1, ui.collection_page + delta)
end

-- Spend tokens to unlock a locked coin for the selected character.
function A.unlock_coin_for(character_id, coin_id)
  if Profile.unlock(ui.profile, character_id, coin_id) then
    Profile.collect(ui.profile, coin_id)
    save_profile()
    return true
  end
  return false
end

function A.unlock_coin(coin_id) return A.unlock_coin_for(ui.selected_character, coin_id) end

-- The coins the selected character starts a run with (its active coin set).
function A.loadout() return Profile.loadout(ui.profile, ui.selected_character, Game.START_MAX, Game.MAX_COPIES) end

-- ---- coin set editor
function A.open_sets(character_id)
  ui.sets_character = character_id or ui.selected_character
  ui.sets_index = Profile.active(ui.profile, ui.sets_character)
  A.go("sets")
end

function A.sets_pick_character(id)
  ui.sets_character = id
  ui.sets_index = Profile.active(ui.profile, id)
end

function A.add_coin_to_set(coin_id)
  if Profile.add_to_set(ui.profile, ui.sets_character, ui.sets_index, coin_id, Game.START_MAX, Game.MAX_COPIES) then
    save_profile()
  end
end

function A.remove_coin_from_set(slot)
  if Profile.remove_from_set(ui.profile, ui.sets_character, ui.sets_index, slot) then save_profile() end
end

function A.clear_set()
  Profile.sets(ui.profile, ui.sets_character)[ui.sets_index].coins = {}
  save_profile()
end

function A.use_set()
  Profile.set_active(ui.profile, ui.sets_character, ui.sets_index)
  save_profile()
end

-- Step through the selected character's sets on the play screen.
function A.cycle_active_set(delta)
  local index = (Profile.active(ui.profile, ui.selected_character) - 1 + delta) % Profile.SET_COUNT + 1
  Profile.set_active(ui.profile, ui.selected_character, index)
  save_profile()
end

-- ---- marking coins to discard (opening hand and bank)
function A.toggle_mark(uid)
  ui.marked[uid] = not ui.marked[uid] or nil
end

local function marked_list(allowed)
  local list = {}
  for _, uid in ipairs(allowed) do if ui.marked[uid] then list[#list + 1] = uid end end
  return list
end

function A.marked_count()
  local game = ui.game
  local pool = game.mulligan and game.mulligan.hand or game.encounter.queue
  return #marked_list(pool)
end

-- Discard every marked coin in one go (does nothing when none are marked).
function A.discard_marked()
  local game = ui.game
  if game.mulligan then
    Game.mulligan_discard(game, marked_list(game.mulligan.hand))
  else
    Game.discard(game, marked_list(game.encounter.queue))
  end
  ui.marked = {}
end

function A.start(seed)
  ui.game = Game.new(seed or (os.time() + math.floor(love.timer.getTime() * 1000000)),
    ui.selected_character, Profile.unlocked_list(ui.profile, ui.selected_character), A.loadout(), true)
  ui.flip_animation = nil
  ui.resolve_timer = 0
  ui.holding = false
  ui.marked = {}
  ui.notice = ""
end

function A.select_character(id)
  ui.selected_character = id
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

-- Step to the previous/next character (wraps around).
function A.cycle_character(delta)
  local order = ui.character_order
  for i, id in ipairs(order) do
    if id == ui.selected_character then
      A.select_character(order[(i - 1 + delta) % #order + 1])
      return
    end
  end
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
    outcome = game.pending.result, elapsed = 0,
    duration = ui.profile.options.fast_flip and .8 or 1.6}
end

function A.next_or_flip()
  if ui.game.mulligan then Game.mulligan_done(ui.game) return end
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
  if game and next(ui.marked) then -- marks only make sense while the coin is still in the bank or hand
    local live = {}
    for _, uid in ipairs(game.mulligan and game.mulligan.hand or game.encounter and game.encounter.queue or {}) do live[uid] = true end
    for uid in pairs(ui.marked) do if not live[uid] then ui.marked[uid] = nil end end
  end
  if game then -- anything that has been in your deck counts as collected
    local fresh = false
    for _, owned in ipairs(game.coins) do fresh = Profile.collect(ui.profile, owned.id) or fresh end
    if fresh then save_profile() end
  end
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
      ui.shake = ui.profile.options.screen_shake and .3 or 0
      ui.resolve_timer = .9
    end
  end
end

return A
