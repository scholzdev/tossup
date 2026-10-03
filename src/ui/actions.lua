-- Player actions and per-frame flow (flip animation -> hold -> resolve).
local Game = require("src.game")
local Profile = require("src.profile")
local ui = require("src.ui.state")

local A = {}

local Lang = require("src.lang")
local Sound = require("src.ui.sound")
local Serialize = require("src.serialize")
local Version = require("src.version")
local Tutorial = require("src.ui.tutorial")

local PROFILE_FILE = "profile.lua"
local RUN_FILE = Game.DEV_MODE and "run-dev.lua" or "run.lua" -- keep development runs separate
local saved_key = nil

local function apply_options()
  love.window.setFullscreen(ui.profile.options.fullscreen)
  Lang.set(ui.profile.options.language)
  Sound.apply(ui.profile.options)
end

function A.load_profile()
  ui.profile = Game.SANDBOX_MODE and Profile.new() or
    Profile.decode(love.filesystem.getInfo(PROFILE_FILE) and love.filesystem.read(PROFILE_FILE))
  if Game.DEV_MODE or Game.SANDBOX_MODE then
    Profile.unlock_all(ui.profile)
    ui.profile.options.seen_help = true
  end
  apply_options()
end

local function save_profile()
  if Game.DEV_MODE or Game.SANDBOX_MODE then return end -- development unlocks and wins never enter the regular profile
  love.filesystem.write(PROFILE_FILE, Profile.encode(ui.profile))
end

-- ---- run saving: the opening hand of a level and the shop are safe points (no coin is mid-flip)
function A.has_saved_run() return not Game.SANDBOX_MODE and love.filesystem.getInfo(RUN_FILE) ~= nil end

local function delete_run()
  if A.has_saved_run() then love.filesystem.remove(RUN_FILE) end
end

local function save_run(game)
  if game.sandbox then return end
  love.filesystem.write(RUN_FILE, Serialize.encode({version = 1, build = Version.build, game = Game.snapshot(game)}))
end

-- Continue the saved run (at the start of its level or in its shop). False when there is none or it cannot be read.
function A.load_run()
  local data = A.has_saved_run() and Serialize.decode(love.filesystem.read(RUN_FILE))
  local ok, game = pcall(function() return type(data) == "table" and data.version == 1 and Game.restore(data.game) end)
  if not ok then game = nil end
  if not game then delete_run() return false end
  game.contracts_enabled = true -- old safe-point saves gain contracts when they reach their next level
  ui.game, ui.selected_character = game, game.character_id
  ui.encounter_reveal = nil
  ui.flip_animation, ui.holding, ui.marked, ui.resolve_timer, ui.notice = nil, false, {}, 0, ""
  saved_key = nil
  return true
end

function A.go(screen)
  ui.screen = screen
  ui.confirm = nil
  ui.collection_page = 1
end

-- "Play" / "New Run": the very first time, show How To Play before the character screen.
function A.play()
  if ui.profile.options.seen_help then A.go("select") return end
  ui.profile.options.seen_help = true
  save_profile()
  Tutorial.start()
end

-- Quitting asks first (popup) while a level is in progress, because only the start of a level and the shop are saved.
function A.quit()
  local g = ui.game
  local running = g and (g.phase == "CONTRACT" or g.phase == "ENCOUNTER" and not g.mulligan) and not g.tutorial -- the level in progress is not saved; levels and the shop are
  if running then
    ui.confirm = {title = "QUIT", text = "THIS LEVEL STARTS OVER WHEN YOU CONTINUE.", ok = love.event.quit}
    return
  end
  love.event.quit()
end

-- One line per finished run, to match tester feedback with seeds (runs.log in the save folder).
local function log_run(game)
  local coins = {}
  for i, owned in ipairs(game.coins) do coins[i] = owned.id end
  love.filesystem.append("runs.log", string.format("%s v=%s seed=%d char=%s stage=%d result=%s cleared=%d gold=%d why=%s coins=%s\n",
    os.date("%Y-%m-%d %H:%M:%S"), Version.number .. "-" .. Version.build, game.seed, game.character_id, game.stake or 1, game.endless and "ENDLESS" or game.phase == "VICTORY" and "WIN" or "LOSS",
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

-- A volume slider (0-100). Saved when the mouse is released (A.save_options).
function A.set_volume(key, value)
  ui.profile.options[key] = math.max(0, math.min(100, math.floor(value + .5)))
  Sound.apply(ui.profile.options)
end

function A.save_options() save_profile() end

-- Put the three volume sliders back to their defaults.
function A.restore_sound_defaults()
  for _, key in ipairs({"volume_master", "volume_music", "volume_sfx"}) do
    ui.profile.options[key] = Profile.DEFAULT_OPTIONS[key]
  end
  Sound.apply(ui.profile.options)
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

function A.continue_endless() Game.continue_endless(ui.game) end

-- Delete unlocks, collection, coin sets and tokens (options stay); also ends a run in progress. Asks first (popup).
function A.clear_progress()
  ui.confirm = {title = "CLEAR PROGRESS", text = "YOUR DATA WILL BE PERMANENTLY DELETED.", ok = A.do_clear_progress}
end

function A.do_clear_progress()
  local options = ui.profile.options
  ui.profile = Profile.new()
  ui.profile.options = options
  if Game.DEV_MODE then Profile.unlock_all(ui.profile) end
  ui.game, ui.set_draft, ui.marked, ui.flip_animation, ui.holding = nil, nil, {}, nil, false
  delete_run()
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
function A.loadout() return Profile.loadout(ui.profile, ui.selected_character, Game.START_MAX) end

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
  character_id = character_id or ui.selected_character
  if not Profile.character_unlocked(ui.profile, character_id) then character_id = "blade" end
  ui.sets_return = ui.screen == "select" and "select" or "title"
  ui.sets_character = character_id
  ui.sets_index = Profile.active(ui.profile, ui.sets_character)
  ui.set_draft = nil
  A.go("sets")
end

function A.back_from_sets()
  A.go(ui.sets_return == "select" and "select" or "title")
end

function A.sets_pick_character(id)
  if not Profile.character_unlocked(ui.profile, id) then return end
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
  if Profile.can_add(ui.profile, ui.sets_character, coins, coin_id, Game.START_MAX) then
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

-- After the opening hand only the coin in play can be discarded (the bank cards are not clickable).
function A.discard_current()
  Game.discard(ui.game)
  ui.marked = {}
end

-- Crystal Ball Tails: click a bank coin to discard it.
function A.discard_bank(uid)
  Game.discard_bank(ui.game, uid)
  ui.marked = {}
end

function A.start(seed)
  if Game.SANDBOX_MODE then return A.start_sandbox() end
  saved_key = nil
  if not Profile.character_unlocked(ui.profile, ui.selected_character) then return end -- win a run with the one before first
  ui.game = Game.new(seed or (os.time() + math.floor(love.timer.getTime() * 1000000)),
    ui.selected_character, Profile.unlocked_list(ui.profile, ui.selected_character), A.loadout(), true, A.stake())
  ui.encounter_reveal = {elapsed = 0, duration = 3.4}
  ui.game.contracts_enabled = true
  Game.offer_contract(ui.game)
  ui.flip_animation = nil
  ui.resolve_timer = 0
  ui.holding = false
  ui.marked = {}
  ui.notice = ""
end

function A.start_sandbox(path)
  path = path or os.getenv("SANDBOX_SCENE") or "sandbox.lua"
  if type(path) ~= "string" or not path:match("^[%w_/%-]+%.lua$") or path:sub(1, 1) == "/" or path:find("//", 1, true) then
    print("invalid sandbox scene file: " .. tostring(path))
    return false
  end
  local module = path:sub(1, -5):gsub("/", ".")
  package.loaded[module] = nil
  local ok, cfg = pcall(require, module)
  if not ok then print(path .. " failed to load: " .. tostring(cfg)) return false end
  local screen = type(cfg) == "table" and (cfg.screen or "encounter")
  local menu_screens = {title = true, select = true, collection = true, sets = true, options = true, help = true}
  if screen ~= "encounter" and screen ~= "shop" and not menu_screens[screen] then
    print(path .. " invalid screen: " .. tostring(screen))
    return false
  end
  local made, game = pcall(Game.new_sandbox, cfg)
  if not made then print(path .. " invalid: " .. tostring(game)) return false end
  if screen == "shop" then Game.open_sandbox_shop(game) end
  game.paused = menu_screens[screen] or false
  saved_key = nil
  ui.game = game
  ui.encounter_reveal = nil
  ui.selected_character = game.character_id
  ui.sets_character = game.character_id
  ui.sets_return = "title"
  ui.set_draft = nil
  ui.tutorial = nil
  A.go(game.paused and screen or "title")
  ui.flip_animation, ui.holding, ui.marked, ui.resolve_timer, ui.notice = nil, false, {}, 0, ""
  return true
end

function A.select_character(id)
  ui.selected_character = id
end

-- The stage (difficulty) picked for the selected character: the highest unlocked one unless the player went lower.
function A.stake()
  local id = ui.selected_character
  local top = Profile.max_stake(ui.profile, id)
  return math.max(1, math.min(top, ui.stake_pick[id] or top))
end

function A.cycle_stake(step)
  local id = ui.selected_character
  ui.stake_pick[id] = math.max(1, math.min(Profile.max_stake(ui.profile, id), A.stake() + step))
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
function A.side_bet(side) return Game.place_side_bet(ui.game, side) end
function A.choose_contract(id) return Game.choose_contract(ui.game, id) end
function A.skip_contract() return Game.skip_contract(ui.game) end
function A.choose_augment(id) return Game.choose_augment(ui.game, id) end

function A.use_item(slot)
  if not ui.flip_animation then Game.use_item(ui.game, slot) end
end

function A.bank_combo()
  if not ui.holding then return 0 end
  local amount = Game.bank_combo(ui.game)
  if amount > 0 then ui.holding = false end
  return amount
end

function A.push_combo()
  if not ui.holding or not Game.can_flip(ui.game) then return false end
  ui.holding = false
  return A.flip_next_coin(true)
end

function A.flip_next_coin(pushing)
  local game = ui.game
  if not Game.flip(game) then return false end
  if pushing then game.encounter.pushing = true end
  if game.tutorial and (game.tutorial_heads or 0) > 0 then -- the scripted tutorial run always shows Heads first
    game.tutorial_heads = game.tutorial_heads - 1
    game.pending.result = "Heads"
  end
  ui.flip_animation = {id = Game.get_coin(game, game.pending.uid).id,
    outcome = game.pending.result, elapsed = 0,
    duration = ui.profile.options.fast_flip and .8 or 1.6}
  return true
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
  if ui.encounter_reveal and game and not game.paused then
    ui.encounter_reveal.elapsed = math.min(ui.encounter_reveal.duration,
      ui.encounter_reveal.elapsed + dt)
  end
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
  do -- autosave at the safe points; drop the save when the run is over
    local safe = game and not game.tutorial and (game.phase == "SHOP" or game.phase == "AUGMENT" or
      ((game.phase == "CONTRACT" or game.phase == "ENCOUNTER") and game.mulligan))
    if safe then
      local key = game.phase .. game.encounter_index .. (game.endless and "e" or "") .. ":" .. game.player.gold .. ":" ..
        #game.coins .. ":" .. game.slots .. ":" .. #game.items .. ":" .. #game.relics .. ":" .. (game.reroll_cost or 0)
      if key ~= saved_key then saved_key = key save_run(game) end -- every shop purchase is saved too (no reroll scumming)
    elseif game and not game.tutorial and (game.phase == "GAME_OVER" or game.phase == "VICTORY") and saved_key ~= "over" then
      saved_key = "over"
      delete_run()
    end
  end
  local record = game and not game.tutorial and game -- a tutorial run is a throwaway: nothing is saved or logged
  if record then -- anything that has been in your deck counts as collected
    local fresh = false
    for _, owned in ipairs(record.coins) do fresh = Profile.collect(ui.profile, owned.id) or fresh end
    if fresh then save_profile() end
  end
  if record and (record.phase == "GAME_OVER" or record.phase == "VICTORY") and not record.tokens_paid then
    record.tokens_paid = Game.run_tokens(record)
    log_run(record)
    ui.profile.tokens = ui.profile.tokens + record.tokens_paid
    save_profile()
  end
  if record and record.phase == "VICTORY" and not record.win_recorded then -- a won run unlocks the next character
    record.win_recorded = true
    record.unlocked_stake = Profile.record_stake_win(ui.profile, record.character_id, record.stake or 1) -- before record_win: its legacy rule would lift max_stake
    record.unlocked_character = Profile.record_win(ui.profile, record.character_id)
    save_profile()
  end
  if record and record.endless and record.phase == "GAME_OVER" and not record.endless_recorded then
    record.endless_recorded = true
    record.endless_record = Profile.record_endless(ui.profile, record.character_id, record.cleared - 8)
    save_profile()
  end
  if record and record.endless and record.phase == "GAME_OVER" and not record.endless_logged then
    record.endless_logged = true
    log_run(record) -- an endless run is logged again when it ends, with the levels cleared beyond the boss
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
