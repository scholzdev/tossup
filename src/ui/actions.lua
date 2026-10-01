-- Player actions and per-frame flow (flip animation -> hold -> resolve).
local Game = require("src.game")
local Profile = require("src.profile")
local ui = require("src.ui.state")

local A = {}

local Lang = require("src.lang")

local PROFILE_FILE = "profile.lua"

local function apply_options()
  love.window.setFullscreen(ui.profile.options.fullscreen)
  Lang.set(ui.profile.options.language)
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
  ui.quit_armed = false
  ui.collection_page = 1
end

-- "Play" / "New Run": the very first time, show How To Play before the character screen.
function A.play()
  if ui.profile.options.seen_help then A.go("select") return end
  ui.profile.options.seen_help = true
  save_profile()
  ui.help_next = "select"
  A.go("help")
end

-- Quit needs a second click while a run is in progress, because runs are not saved.
function A.quit()
  local g = ui.game
  local running = g and g.phase ~= "GAME_OVER" and g.phase ~= "VICTORY"
  if running and not ui.quit_armed then ui.quit_armed = true return end
  love.event.quit()
end

-- One line per finished run, to match tester feedback with seeds (runs.log in the save folder).
local function log_run(game)
  local coins = {}
  for i, owned in ipairs(game.coins) do coins[i] = owned.id end
  love.filesystem.append("runs.log", string.format("%s seed=%d char=%s result=%s cleared=%d gold=%d why=%s coins=%s\n",
    os.date("%Y-%m-%d %H:%M:%S"), game.seed, game.character_id, game.phase == "VICTORY" and "WIN" or "LOSS",
    game.cleared, game.player.gold, game.lost_why or "-", table.concat(coins, ",")))
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

-- Cycle through the available languages (English, Deutsch).
function A.cycle_language()
  local order = Lang.order
  for i, code in ipairs(order) do
    if code == Lang.current then
      ui.profile.options.language = order[i % #order + 1]
      break
    end
  end
  Lang.set(ui.profile.options.language)
  save_profile()
end

function A.set_filter(rarity)
  ui.collection_filter = rarity
  ui.collection_page = 1
end

function A.change_collection_page(delta)
  ui.collection_page = math.max(1, ui.collection_page + delta)
end

-- The coins the selected character starts a run with (its active coin set).
function A.loadout() return Profile.loadout(ui.profile, ui.selected_character, Game.START_MAX, Game.MAX_COPIES) end

-- ---- coin set editor: edits go to a draft and only reach the profile when Save is pressed
-- (LÖVE runs LuaJIT, which has no table.unpack, so lists are copied by hand)
local function copy_list(list)
  local out = {}
  for i, id in ipairs(list) do out[i] = id end
  return out
end

local function same_list(a, b)
  if #a ~= #b then return false end
  for i = 1, #a do if a[i] ~= b[i] then return false end end
  return true
end

-- The coins of the open set as currently edited (a copy of the saved set until something changes).
function A.set_draft()
  local d = ui.set_draft
  if not d or d.character ~= ui.sets_character or d.index ~= ui.sets_index then
    local saved = Profile.sets(ui.profile, ui.sets_character)[ui.sets_index].coins
    d = {character = ui.sets_character, index = ui.sets_index, coins = copy_list(saved)}
    ui.set_draft = d
  end
  return d.coins
end

function A.set_dirty()
  local saved = Profile.sets(ui.profile, ui.sets_character)[ui.sets_index].coins
  return not same_list(A.set_draft(), saved)
end

function A.open_sets(character_id)
  ui.sets_character = character_id or ui.selected_character
  ui.sets_index = Profile.active(ui.profile, ui.sets_character)
  ui.set_draft = nil
  A.go("sets")
end

function A.sets_pick_character(id)
  ui.sets_character = id
  ui.sets_index = Profile.active(ui.profile, id)
  ui.set_draft = nil
end

function A.sets_pick_set(index)
  ui.sets_index = index
  ui.set_draft = nil -- unsaved edits are dropped when you switch sets
  Profile.set_active(ui.profile, ui.sets_character, index) -- the play screen opens on the set you last looked at
  save_profile()
end

function A.add_coin_to_set(coin_id)
  local coins = A.set_draft()
  if Profile.can_add(ui.profile, ui.sets_character, coins, coin_id, Game.START_MAX, Game.MAX_COPIES) then
    coins[#coins + 1] = coin_id
  end
end

function A.remove_coin_from_set(slot)
  table.remove(A.set_draft(), slot)
end

function A.clear_set()
  ui.set_draft = {character = ui.sets_character, index = ui.sets_index, coins = {}}
end

function A.save_set()
  Profile.sets(ui.profile, ui.sets_character)[ui.sets_index].coins = copy_list(A.set_draft())
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

-- Leave the level for the shop; only possible once the quota is met.
function A.open_shop()
  if not ui.flip_animation then Game.end_level(ui.game) end
end

function A.exchange() Game.exchange(ui.game) end
function A.give_up() Game.give_up(ui.game) end

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
  if game and next(game.purchased) then -- buying a locked coin in the shop unlocks it for good
    local unlocked_now = false
    for id in pairs(game.purchased) do
      unlocked_now = Profile.grant(ui.profile, game.character_id, id) or unlocked_now
    end
    game.purchased = {}
    if unlocked_now then save_profile() end
  end
  if game then -- anything that has been in your deck counts as collected
    local fresh = false
    for _, owned in ipairs(game.coins) do fresh = Profile.collect(ui.profile, owned.id) or fresh end
    if fresh then save_profile() end
  end
  if game and (game.phase == "GAME_OVER" or game.phase == "VICTORY") and not game.tokens_paid then
    game.tokens_paid = Game.run_tokens(game)
    log_run(game)
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
