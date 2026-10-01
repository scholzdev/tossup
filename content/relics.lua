-- Relic registry: content/relics/<id>.lua. Hook reference: src/relics.lua.
local ORDER = {"magnet", "penny", "clock", "metronome", "baton"}

local defs = {}
for _, id in ipairs(ORDER) do defs[id] = require("content.relics." .. id) end
return defs
