-- Item registry: content/items/<id>.lua. Hook reference: src/items.lua.
local ORDER = {"force_heads", "force_tails", "weighted", "double_down", "swap", "peek", "extra_draw"}

local defs = {}
for _, id in ipairs(ORDER) do defs[id] = require("content.items." .. id) end
return defs
