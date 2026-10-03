-- Text lookup. Source strings in the code are English; locales/<code>.lua maps them to another language.
-- Lang.t("BACK") -> "ZURÜCK" (German). Unknown strings come back unchanged, so English needs no table.
-- Lang.t("LEVEL %d / 8", n) formats after the lookup, so the translation can move the number.
-- Names and descriptions of coins, chips, prizes and characters live in the table's coins/items/relics/characters.
local Lang = {current = "en", names = {en = "ENGLISH", de = "DEUTSCH"}, order = {"en", "de"}}

local tables = {en = {}, de = require("locales.de")}

function Lang.set(code) if tables[code] then Lang.current = code end end

function Lang.t(text, ...)
  if type(text) ~= "string" then return text end
  text = tables[Lang.current][text] or text
  if select("#", ...) > 0 then return string.format(text, ...) end
  return text
end

-- A def table (coins, chips, ...) whose name/description/short are looked up in the current language.
function Lang.wrap(set, defs)
  local out = {}
  for id, def in pairs(defs) do
    out[id] = setmetatable({}, {__index = function(_, key)
      local group = tables[Lang.current][set]
      local entry = group and group[id]
      return entry and entry[key] or def[key]
    end})
  end
  return out
end

-- string.upper only knows ASCII: teach it the German umlauts so "Münze" -> "MÜNZE".
local ascii_upper = string.upper
local UMLAUT = {["\164"] = "\132", ["\182"] = "\150", ["\188"] = "\156"} -- ä ö ü -> Ä Ö Ü (second byte)
function string.upper(s)
  return (ascii_upper(s):gsub("\195([\164\182\188])", function(b) return "\195" .. UMLAUT[b] end))
end

-- Split a UTF-8 string into characters (for the vertical shop labels).
function Lang.chars(s)
  local list = {}
  for ch in s:gmatch("[%z\1-\127\194-\244][\128-\191]*") do list[#list + 1] = ch end
  return list
end

return Lang
