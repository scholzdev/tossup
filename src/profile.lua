-- Meta progression that survives between runs: tokens, coin unlocks, coin sets and options.
-- Pure data + (de)serialization; the LÖVE layer decides where the string is stored.
local characters = require("content.characters")

local Profile = {}

Profile.SET_COUNT = 3 -- coin sets per character
Profile.SET_SIZE = 5 -- coins in a set (Game.START_MAX); older, longer sets are cut to this when loaded

local DEFAULT_OPTIONS = {screen_shake = true, fast_flip = false, fullscreen = false, seen_help = false, language = "en", volume_master = 80, volume_music = 40, volume_sfx = 80}

Profile.DEFAULT_OPTIONS = DEFAULT_OPTIONS

function Profile.new()
  local options = {}
  for key, value in pairs(DEFAULT_OPTIONS) do options[key] = value end
  return {tokens = 0, unlocked = {}, collected = {}, sets = {}, active_set = {}, options = options, wins = {}, best_endless = {}, stakes = {}}
end

-- Characters unlock in this order: the first is always playable, each next one after winning a run with the one before.
Profile.CHARACTER_ORDER = {"blade", "seer", "trader"}

function Profile.character_unlocked(profile, character_id)
  for i, id in ipairs(Profile.CHARACTER_ORDER) do
    if id == character_id then return i == 1 or profile.wins[Profile.CHARACTER_ORDER[i - 1]] == true end
  end
  return false
end

-- The character that must be beaten to unlock this one (nil for the first).
function Profile.required_for(character_id)
  for i, id in ipairs(Profile.CHARACTER_ORDER) do
    if id == character_id then return Profile.CHARACTER_ORDER[i - 1] end
  end
end

-- A run was won with this character. Returns the id of the character it unlocked, if it unlocked one.
function Profile.record_win(profile, character_id)
  if profile.wins[character_id] then return nil end
  profile.wins[character_id] = true
  for i, id in ipairs(Profile.CHARACTER_ORDER) do
    if id == character_id then return Profile.CHARACTER_ORDER[i + 1] end
  end
end

-- Stages (difficulty, content/stakes.lua): the highest one a character may start. Winning a run on the highest unlocks the next.
-- Profiles from before stages: a character that has won already may start stage 2.
function Profile.max_stake(profile, character_id)
  return profile.stakes[character_id] or (profile.wins[character_id] and 2 or 1)
end

-- A run was won on this stage. Returns the stage it unlocked, if it unlocked one.
function Profile.record_stake_win(profile, character_id, stake)
  local top = #require("content.stakes")
  if stake >= Profile.max_stake(profile, character_id) and stake < top then
    profile.stakes[character_id] = stake + 1
    return stake + 1
  end
end

-- Endless levels cleared beyond the boss; keeps the best per character. Returns true for a new record.
function Profile.record_endless(profile, character_id, levels)
  if levels > 0 and levels > (profile.best_endless[character_id] or 0) then
    profile.best_endless[character_id] = levels
    return true
  end
  return false
end

-- Mark a coin as seen in the collection. Returns true if it was new.
function Profile.collect(profile, coin_id)
  if profile.collected[coin_id] then return false end
  profile.collected[coin_id] = true
  return true
end

-- Extra coin ids this character may sell in the shop (for Game.new).
function Profile.unlocked_list(profile, character_id)
  local list = {}
  for id in pairs(profile.unlocked[character_id] or {}) do list[#list + 1] = id end
  table.sort(list)
  return list
end

function Profile.is_unlocked(profile, character_id, coin_id)
  return (profile.unlocked[character_id] or {})[coin_id] == true
end

-- Unlock a coin of this character's locked list (done when it is bought in the shop). Returns
-- true if it was newly unlocked.
function Profile.grant(profile, character_id, coin_id)
  for _, entry in ipairs(characters[character_id].locked or {}) do
    if entry[1] == coin_id and not Profile.is_unlocked(profile, character_id, coin_id) then
      profile.unlocked[character_id] = profile.unlocked[character_id] or {}
      profile.unlocked[character_id][coin_id] = true
      return true
    end
  end
  return false
end

-- Coins this character may bring or buy: its base pool plus tokens-unlocked coins.
local function available(profile, character_id)
  local set = {}
  for _, id in ipairs(characters[character_id].pool) do set[id] = true end
  for id in pairs(profile.unlocked[character_id] or {}) do set[id] = true end
  return set
end

local function copy(list)
  local out = {}
  for i, id in ipairs(list) do out[i] = id end
  return out
end

-- The character's coin sets, created on first use: set 1 is the default deck, the others start
-- empty. Returns a list of {name, coins}.
function Profile.sets(profile, character_id)
  local sets = profile.sets[character_id]
  if not sets then
    local def = characters[character_id]
    sets = {{name = "SET 1", coins = copy(def.deck or {def.starter})}}
    for i = 2, Profile.SET_COUNT do sets[i] = {name = "SET " .. i, coins = {}} end
    profile.sets[character_id] = sets
  end
  return sets
end

function Profile.active(profile, character_id)
  local index = profile.active_set[character_id] or 1
  return math.max(1, math.min(Profile.SET_COUNT, index))
end

function Profile.set_active(profile, character_id, index)
  if index >= 1 and index <= Profile.SET_COUNT then profile.active_set[character_id] = index end
end

-- Add one coin to a set if it is available, the set has room, and the copy limit allows it.
-- max_copies does not apply to the plain Normal coin.
function Profile.can_add(profile, character_id, coins, coin_id, max, max_copies)
  if #coins >= max or not available(profile, character_id)[coin_id] then return false end
  local copies = 0
  for _, id in ipairs(coins) do if id == coin_id then copies = copies + 1 end end
  return coin_id == "normal" or copies < max_copies
end

function Profile.add_to_set(profile, character_id, index, coin_id, max, max_copies)
  local coins = Profile.sets(profile, character_id)[index].coins
  if not Profile.can_add(profile, character_id, coins, coin_id, max, max_copies) then return false end
  coins[#coins + 1] = coin_id
  return true
end

function Profile.remove_from_set(profile, character_id, index, slot)
  local coins = Profile.sets(profile, character_id)[index].coins
  if not coins[slot] then return false end
  table.remove(coins, slot)
  return true
end

-- The coins a new run starts with: the active set, limited to coins that are available, to max
-- entries and to max_copies of a coin (Normal is exempt), so an old or hand-edited save can never
-- produce a set the game would reject. An empty (or unusable) set falls back to the default deck.
function Profile.loadout(profile, character_id, max, max_copies)
  max_copies = max_copies or max
  local def = characters[character_id]
  local ok = available(profile, character_id)
  local function pick(source)
    local list, copies = {}, {}
    for _, id in ipairs(source) do
      copies[id] = (copies[id] or 0) + 1
      if ok[id] and #list < max and (id == "normal" or copies[id] <= max_copies) then list[#list + 1] = id end
    end
    return list
  end
  local sets = Profile.sets(profile, character_id)
  local list = pick(sets[Profile.active(profile, character_id)].coins)
  if #list == 0 then list = pick(def.deck or {def.starter}) end
  return list
end

local function quote_list(list)
  local quoted = {}
  for i, id in ipairs(list) do quoted[i] = string.format("%q", id) end
  return table.concat(quoted, ", ")
end

local function sorted_keys(t)
  local keys = {}
  for key in pairs(t) do keys[#keys + 1] = key end
  table.sort(keys)
  return keys
end

function Profile.encode(profile)
  local lines = {"return {tokens = " .. profile.tokens .. ", unlocked = {"}
  for _, character_id in ipairs(sorted_keys(profile.unlocked)) do
    local ids = {}
    for _, id in ipairs(Profile.unlocked_list(profile, character_id)) do ids[#ids + 1] = id .. " = true" end
    lines[#lines + 1] = "  " .. character_id .. " = {" .. table.concat(ids, ", ") .. "},"
  end
  lines[#lines + 1] = "}, collected = {"
  for _, id in ipairs(sorted_keys(profile.collected)) do lines[#lines + 1] = "  " .. id .. " = true," end
  lines[#lines + 1] = "}, sets = {"
  for _, character_id in ipairs(sorted_keys(profile.sets)) do
    lines[#lines + 1] = "  " .. character_id .. " = {"
    for _, set in ipairs(profile.sets[character_id]) do
      lines[#lines + 1] = string.format("    {name = %q, coins = {%s}},", set.name, quote_list(set.coins))
    end
    lines[#lines + 1] = "  },"
  end
  lines[#lines + 1] = "}, active_set = {"
  for _, character_id in ipairs(sorted_keys(profile.active_set)) do
    lines[#lines + 1] = "  " .. character_id .. " = " .. profile.active_set[character_id] .. ","
  end
  lines[#lines + 1] = "}, wins = {"
  for _, id in ipairs(sorted_keys(profile.wins)) do lines[#lines + 1] = "  " .. id .. " = true," end
  lines[#lines + 1] = "}, stakes = {"
  for _, id in ipairs(sorted_keys(profile.stakes)) do lines[#lines + 1] = "  " .. id .. " = " .. profile.stakes[id] .. "," end
  lines[#lines + 1] = "}, best_endless = {"
  for _, id in ipairs(sorted_keys(profile.best_endless)) do lines[#lines + 1] = "  " .. id .. " = " .. profile.best_endless[id] .. "," end
  lines[#lines + 1] = "}, options = {"
  for _, key in ipairs(sorted_keys(profile.options)) do
    lines[#lines + 1] = "  " .. key .. " = " .. tostring(profile.options[key]) .. ","
  end
  lines[#lines + 1] = "}}"
  return table.concat(lines, "\n")
end

-- Returns a fresh profile if the text is missing or malformed.
function Profile.decode(text)
  local chunk = text and load(text, "profile", "t", {})
  local ok, data = pcall(chunk or function() end)
  if not ok or type(data) ~= "table" or type(data.tokens) ~= "number" or type(data.unlocked) ~= "table" then
    return Profile.new()
  end
  -- fill what older save files lack
  local fresh = Profile.new()
  data.collected = type(data.collected) == "table" and data.collected or fresh.collected
  data.sets = type(data.sets) == "table" and data.sets or fresh.sets
  for _, list in pairs(data.sets) do -- sets used to hold 10 coins
    for _, set in ipairs(type(list) == "table" and list or {}) do
      while type(set.coins) == "table" and #set.coins > Profile.SET_SIZE do table.remove(set.coins) end
    end
  end
  data.wins = type(data.wins) == "table" and data.wins or fresh.wins
  data.stakes = type(data.stakes) == "table" and data.stakes or fresh.stakes
  data.best_endless = type(data.best_endless) == "table" and data.best_endless or fresh.best_endless
  data.active_set = type(data.active_set) == "table" and data.active_set or fresh.active_set
  -- older saves kept a single loadout per character: it becomes set 1
  if type(data.loadouts) == "table" then
    for character_id, list in pairs(data.loadouts) do
      if characters[character_id] and not data.sets[character_id] and type(list) == "table" then
        Profile.sets(data, character_id)[1].coins = copy(list)
      end
    end
    data.loadouts = nil
  end
  data.options = type(data.options) == "table" and data.options or {}
  for key, value in pairs(DEFAULT_OPTIONS) do
    if type(data.options[key]) ~= type(value) then data.options[key] = value end
  end
  return data
end

return Profile
