-- Meta progression that survives between runs: tokens and per-character coin unlocks.
-- Pure data + (de)serialization; the LÖVE layer decides where the string is stored.
local characters = require("content.characters")

local Profile = {}

local DEFAULT_OPTIONS = {screen_shake = true, fast_flip = false, fullscreen = false}

function Profile.new()
  local options = {}
  for key, value in pairs(DEFAULT_OPTIONS) do options[key] = value end
  return {tokens = 0, unlocked = {}, collected = {}, options = options}
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

-- Buy a locked coin with tokens. Returns true on success.
function Profile.unlock(profile, character_id, coin_id)
  for _, entry in ipairs(characters[character_id].locked or {}) do
    local id, cost = entry[1], entry[2]
    if id == coin_id and not Profile.is_unlocked(profile, character_id, id) and profile.tokens >= cost then
      profile.tokens = profile.tokens - cost
      profile.unlocked[character_id] = profile.unlocked[character_id] or {}
      profile.unlocked[character_id][id] = true
      return true
    end
  end
  return false
end

function Profile.encode(profile)
  local lines = {"return {tokens = " .. profile.tokens .. ", unlocked = {"}
  local chars = {}
  for character_id in pairs(profile.unlocked) do chars[#chars + 1] = character_id end
  table.sort(chars)
  for _, character_id in ipairs(chars) do
    local ids = {}
    for _, id in ipairs(Profile.unlocked_list(profile, character_id)) do ids[#ids + 1] = id .. " = true" end
    lines[#lines + 1] = "  " .. character_id .. " = {" .. table.concat(ids, ", ") .. "},"
  end
  lines[#lines + 1] = "}, collected = {"
  local ids = {}
  for id in pairs(profile.collected) do ids[#ids + 1] = id end
  table.sort(ids)
  for _, id in ipairs(ids) do lines[#lines + 1] = "  " .. id .. " = true," end
  lines[#lines + 1] = "}, options = {"
  local keys = {}
  for key in pairs(profile.options) do keys[#keys + 1] = key end
  table.sort(keys)
  for _, key in ipairs(keys) do lines[#lines + 1] = "  " .. key .. " = " .. tostring(profile.options[key]) .. "," end
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
  data.options = type(data.options) == "table" and data.options or {}
  for key, value in pairs(DEFAULT_OPTIONS) do
    if type(data.options[key]) ~= type(value) then data.options[key] = value end
  end
  return data
end

return Profile
