-- Coin effect hooks. Routes a coin def's hooks onto the Signal bus, scoped to the coin that is
-- currently dealt/flipped. Modelled on the marble hooks in carnival_game (engine/marble.lua).
--
-- A coin def (content/coins/<id>.lua) may declare hooks in two styles, mixed freely:
--
--   1. sugar -- one function per hook:
--        on_deal(game, inst)             coin was dealt from the stack (before you flip or discard)
--        on_flip(game, inst, flip)       dice rolled. flip = {raw, result, probability}; set
--                                        flip.result to change the outcome (Heads/Tails)
--        on_resolve(game, inst, res)     outcome final, effects NOT yet applied. res = {result,
--                                        raw, effects}; res.effects is a private copy: edit it,
--                                        add to it, or empty it
--        on_discard(game, inst)          coin discarded for the level
--
--   2. register(ctx) -- subscribe yourself, for several events or closure state:
--        register = function(ctx)
--          ctx.on("coin_resolve", function(e) ... end)
--        end
--      ctx = {game, inst, on}. Handlers are torn down when the coin resolves or is discarded.
--
-- Pure (no side effects), evaluated any time odds are shown, for every owned coin:
--   on_odds(game, inst, odds)           odds = {p}; mutate odds.p
--
-- Persistent growth:
--   grow(inst, event)                   event = "level" (each level start, every owned coin)
--                                       | "flip" (each time this coin resolves)
--                                       | "discard"
--   `inst` is the owned instance {uid, id, bonus, ...} and lives for the whole run, so counters
--   on it are how a coin scales itself.
--
-- Events (scoped ones carry {game, inst}; all are also on the bus for relics/characters):
--   coin_deal  coin_flip{flip}  coin_resolve{res}  coin_discard
--   effect_applied{effect, text}  coin_resolved{res}             -- after the effects ran
--   encounter_start{encounter}  encounter_end{won}                 -- global, no inst
--
-- Helpers for hooks: Game.log(game, msg), Game.apply_effect(game, inst, effect). A coin file must
-- require("src.game") lazily inside the hook, not at file top (src.game loads the coin defs).
-- Randomness inside a hook must use RNG.random(game) / RNG.int(game, a, b) to stay seeded.
local Signal = require("src.signal")
local catalog = require("content.coins")

local Hooks = {}

-- sugar name -> {event, unpacker to the sugar argument order}
local SUGAR = {
  on_deal = {"coin_deal", function(e) return e.game, e.inst end},
  on_flip = {"coin_flip", function(e) return e.game, e.inst, e.flip end},
  on_resolve = {"coin_resolve", function(e) return e.game, e.inst, e.res end},
  on_discard = {"coin_discard", function(e) return e.game, e.inst end},
}

local active -- one coin is in play at a time, so a single handle list is enough

function Hooks.unbind()
  if not active then return end
  for _, handle in ipairs(active) do Signal.off(handle) end
  active = nil
end

function Hooks.bind(game, inst)
  Hooks.unbind()
  local def = catalog[inst.id]
  local handles = {}
  active = handles
  local function on(event, handler)
    handles[#handles + 1] = Signal.on(event, function(e)
      if e.inst == inst then handler(e) end
    end)
  end
  for hook, spec in pairs(SUGAR) do
    local fn = def[hook]
    if fn then
      local unpack_args = spec[2]
      on(spec[1], function(e) fn(unpack_args(e)) end)
    end
  end
  if def.register then def.register({game = game, inst = inst, on = on}) end
end

function Hooks.grow(inst, event)
  local grow = catalog[inst.id].grow
  if grow then grow(inst, event) end
end

function Hooks.odds(game, inst, p)
  local on_odds = catalog[inst.id].on_odds
  if not on_odds then return p end
  local odds = {p = p}
  on_odds(game, inst, odds)
  return odds.p
end

return Hooks
