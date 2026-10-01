-- Dump the game's content (coins, chips, prizes, modifiers, characters, constants, German texts) as JSON on stdout.
-- Used by tools/build_wiki.sh; plain Lua, no LÖVE.   Run from the repo root:  lua tools/dump_content.lua > content.json
package.path = "./?.lua;" .. package.path
local Game = require("src.game")
local order = require("content.coin_order")
local de = require("locales.de")

local out = {}
local function w(s) out[#out + 1] = s end

local function is_array(t)
  local n = 0
  for _ in pairs(t) do n = n + 1 end
  for i = 1, n do if t[i] == nil then return false end end
  return true
end

local function json(value)
  local kind = type(value)
  if kind == "nil" or kind == "function" or kind == "userdata" then return "null" end
  if kind == "boolean" then return tostring(value) end
  if kind == "number" then
    if value == math.floor(value) then return string.format("%d", value) end
    return string.format("%.6g", value)
  end
  if kind == "string" then
    return '"' .. value:gsub('[%c"\\]', function(c)
      local map = {['"'] = '\\"', ["\\"] = "\\\\", ["\n"] = "\\n", ["\r"] = "\\r", ["\t"] = "\\t"}
      return map[c] or string.format("\\u%04x", c:byte())
    end) .. '"'
  end
  local parts = {}
  if is_array(value) then
    for i = 1, #value do parts[#parts + 1] = json(value[i]) end
    return "[" .. table.concat(parts, ",") .. "]"
  end
  local keys = {}
  for k, v in pairs(value) do
    if type(v) ~= "function" and type(v) ~= "userdata" then keys[#keys + 1] = tostring(k) end
  end
  table.sort(keys)
  for _, k in ipairs(keys) do
    local v = value[k] ~= nil and value[k] or value[tonumber(k)]
    parts[#parts + 1] = json(k) .. ":" .. json(v)
  end
  return "{" .. table.concat(parts, ",") .. "}"
end

local HOOKS = {"on_deal", "on_flip", "on_resolve", "on_discard", "on_odds", "grow", "register"}

-- keep data fields only; remember which hooks a def has
local function plain(def)
  local copy, hooks = {}, {}
  for k, v in pairs(def) do
    if type(v) ~= "function" then copy[k] = v end
  end
  for _, name in ipairs(HOOKS) do if def[name] then hooks[#hooks + 1] = name end end
  copy.hooks = hooks
  return copy
end

local function defs_of(map, ids)
  local result = {}
  for _, id in ipairs(ids) do
    local entry = plain(map[id])
    entry.id = id
    result[#result + 1] = entry
  end
  return result
end

local function sorted_ids(map)
  local ids = {}
  for id in pairs(map) do ids[#ids + 1] = id end
  table.sort(ids)
  return ids
end

local characters = {}
for _, id in ipairs({"blade", "seer", "trader"}) do
  local def = Game.characters()[id]
  local locked = {}
  for _, entry in ipairs(def.locked or {}) do locked[#locked + 1] = entry[1] end
  characters[#characters + 1] = {id = id, name = def.name, description = def.description, deck = def.deck, pool = def.pool, locked = locked}
end

local route = {}
for i, stage in ipairs(Game.route) do route[i] = {name = stage.name, per_coin = stage.per_coin, payout = stage.payout, boss = stage.boss or false} end

local constants = {}
for _, key in ipairs({"START_GOLD", "START_MAX", "DECK_MAX", "SLOT_COST", "EXCHANGE_BASE", "EXCHANGE_STEP", "EXCHANGE_GAIN", "EXCHANGE_MAX",
  "SURPLUS_RATE", "COMBO_STEP", "COMBO_CAP", "RETURN_CAP", "MAX_COPIES", "VISIBLE", "MULLIGAN"}) do
  constants[key] = Game[key]
end

local modifier_ids = {"lucky_day", "cold_snap", "power_surge", "blackout", "gold_rush", "high_stakes", "good_rhythm", "bonus_exchange"}

w(json({
  coins = defs_of(Game.catalog(), order),
  items = defs_of(Game.item_catalog(), (function()
    local ids = sorted_ids(Game.item_catalog())
    return ids
  end)()),
  relics = defs_of(Game.relics(), sorted_ids(Game.relics())),
  modifiers = defs_of(Game.modifiers(), modifier_ids),
  characters = characters,
  route = route,
  constants = constants,
  de = {coins = de.coins, items = de.items, relics = de.relics, modifiers = de.modifiers, characters = de.characters},
  de_strings = (function() -- the flat English -> German table (effect texts, rarity names, ...)
    local flat = {}
    for k, v in pairs(de) do if type(v) == "string" then flat[k] = v end end
    return flat
  end)(),
}))
io.write(table.concat(out))
