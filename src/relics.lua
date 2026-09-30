-- Relics are passive, run-long modifiers. Each relic def has register(ctx) with
-- ctx = {game, on}; on(event, handler) subscribes to the Signal bus while the relic is owned.
--
-- Events relics usually want (all carry {game, ...}):
--   encounter_start{encounter}   encounter_end{won}
--   coin_deal{inst}  coin_flip{inst, flip}  coin_outcome{inst, flips, result}  <- set e.result
--   coin_resolve{inst, res}  effect_applied{inst, effect, text}  coin_resolved{inst, res}
-- coin_outcome fires before the boss inversion; set e.result to "Heads"/"Tails" to change it.
local Signal = require("src.signal")
local catalog = require("content.relics")

local Relics = {}

local handles = {}

function Relics.unbind()
  for _, handle in ipairs(handles) do Signal.off(handle) end
  handles = {}
end

-- Rebind every owned relic of this game (call after the relic list changes).
function Relics.bind(game)
  Relics.unbind()
  local function on(event, handler)
    handles[#handles + 1] = Signal.on(event, function(e)
      if e.game == game then handler(e) end
    end)
  end
  for _, id in ipairs(game.relics) do
    catalog[id].register({game = game, on = on})
  end
end

return Relics
