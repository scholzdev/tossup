-- Consumable items: bought in the shop, used in the middle of a level while a coin is dealt.
-- Item def (content/items/<id>.lua): name, short, description, cost, use(game, Items).
-- use may return false to refuse (the item is then not consumed). To change the coming flip,
-- arm a one-shot listener: Items.arm("coin_flip", function(e) e.flip.result = "Heads" end).
-- Armed listeners are dropped at encounter end and when a new game starts.
local Signal = require("src.signal")
local catalog = require("content.items")

local Items = {}
Items.MAX = 3

local armed = {}

function Items.clear()
  for _, handle in ipairs(armed) do Signal.off(handle) end
  armed = {}
end

-- Run fn once on the next emit of event.
function Items.arm(event, fn)
  local handle
  handle = Signal.on(event, function(e)
    Signal.off(handle)
    for i, h in ipairs(armed) do
      if h == handle then table.remove(armed, i) break end
    end
    fn(e)
  end)
  armed[#armed + 1] = handle
end

function Items.can_use(game)
  return game.phase == "ENCOUNTER" and game.dealt ~= nil and game.pending == nil
end

function Items.use(game, slot)
  local id = game.items[slot]
  if not id or not Items.can_use(game) then return false end
  if catalog[id].use(game, Items) == false then return false end
  table.remove(game.items, slot)
  require("src.game").log(game, "Used " .. catalog[id].name .. ".")
  return true
end

return Items
