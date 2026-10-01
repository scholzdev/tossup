-- Headless balance simulator. Run from the repo root:
--   lua tools/sim.lua                      all bots x all characters, 300 runs each
--   lua tools/sim.lua --runs 1000 --char blade --bot smart
--   lua tools/sim.lua --unlock all         pretend every locked coin is unlocked
--   lua tools/sim.lua --set default        start from the default deck instead of a best-coins 10-coin set
--   lua tools/sim.lua --coins              per-coin strength report
--   lua tools/sim.lua --trace 7 --char blade --bot smart   print the full log of one run
--   lua tools/sim.lua --quota 3,8,14,26 --payout 20,25,30   override the route
-- Everything is seeded: the same arguments always print the same numbers.
package.path = "./?.lua;" .. package.path
local Game = require("src.game")
local Signal = require("src.signal")
local Items = require("src.items")

local catalog, item_catalog, characters = Game.catalog(), Game.item_catalog(), Game.characters()

-- ---------------------------------------------------------------- args
local opts = {runs = 300, bot = "all", char = "all"}
local i = 1
while arg and arg[i] do
  local a = arg[i]
  if a == "--runs" then opts.runs = tonumber(arg[i + 1]) i = i + 1
  elseif a == "--bot" then opts.bot = arg[i + 1] i = i + 1
  elseif a == "--char" then opts.char = arg[i + 1] i = i + 1
  elseif a == "--unlock" then opts.unlock = arg[i + 1] i = i + 1
  elseif a == "--quota" then opts.quota = arg[i + 1] i = i + 1
  elseif a == "--payout" then opts.payout = arg[i + 1] i = i + 1
  elseif a == "--set" then opts.set = arg[i + 1] i = i + 1
  elseif a == "--coins" then opts.coins = true
  elseif a == "--trace" then opts.trace = tonumber(arg[i + 1]) i = i + 1
  else error("unknown argument: " .. a) end
  i = i + 1
end

local function numbers(text)
  local list = {}
  for n in text:gmatch("[^,]+") do list[#list + 1] = tonumber(n) end
  return list
end
if opts.quota then
  for level, n in ipairs(numbers(opts.quota)) do Game.route[level].per_coin = n end
end
if opts.payout then
  for level, n in ipairs(numbers(opts.payout)) do Game.route[level].payout = n end
end

-- ---------------------------------------------------------------- coin strength
-- Value of a coin = points it scores per flip, minus quota penalties, plus a discount on the rest.
local function value_of(points, gold, penalty, energy) return points - penalty + .5 * gold + .3 * energy end
local ENERGY_WORTH = .5 -- what one energy is worth in points when a coin charges for it

-- Flip a coin many times in a neutral deck (X, sword, dagger, normal, normal) and total what its
-- own effects did. Context-dependent coins (Echo, Chain, Flux Capacitor...) are measured in that context.
local function measure(id, games, flips)
  characters.sim = {name = "Sim", description = "", starter = "normal", pool = {"normal", "sword", "dagger", "hammer"},
    deck = {id, "sword", "dagger", "normal", "normal"}, locked = {}}
  local totals = {points = 0, gold = 0, penalty = 0, energy = 0, flips = 0}
  local handles = {
    Signal.on("effect_applied", function(e)
      if e.inst.id ~= id then return end
      local t, amount = e.effect.type, e.effect.amount
      if t == "score" then totals.points = totals.points + amount
      elseif t == "gold" then totals.gold = totals.gold + amount
      elseif t == "penalty" then totals.penalty = totals.penalty + amount
      elseif t == "energy" then totals.energy = totals.energy + amount end
    end),
    Signal.on("coin_resolved", function(e) if e.inst.id == id then totals.flips = totals.flips + 1 end end),
  }
  for seed = 1, games do
    local g = Game.new(seed, "sim")
    g.player.gold = 30
    for step = 1, flips do
      g.encounter.quota, g.encounter.max_quota = 1e9, 1e9
      if step % 15 == 1 then g.encounter.doubler = nil end -- Doubler's counter is per level (about three passes of a deck)
      g.reshuffle = true -- measuring the coin, not running out of coins
      g.player.energy = math.max(g.player.energy, 3) -- measuring the coin, not the energy economy
      if g.dealt then Game.flip(g) Game.resolve(g) end
    end
  end
  for _, h in ipairs(handles) do Signal.off(h) end
  characters.sim = nil
  local n = math.max(1, totals.flips)
  local cost = catalog[id].energy_cost or 0
  return {points = totals.points / n, gold = totals.gold / n, penalty = totals.penalty / n,
    energy = totals.energy / n, cost = cost,
    value = value_of(totals.points / n, totals.gold / n, totals.penalty / n, totals.energy / n) - ENERGY_WORTH * cost}
end

local measured = {}
local function coin_value(id)
  if not measured[id] then measured[id] = measure(id, 40, 100) end
  return measured[id].value
end

local function coin_report()
  local order = require("content.coin_order")
  local groups, rows = {}, {}
  for _, id in ipairs(order) do
    local m = measure(id, 100, 100)
    local rarity = catalog[id].rarity or "?"
    rows[#rows + 1] = {id = id, rarity = rarity, cost = catalog[id].cost or 15, m = m}
    groups[rarity] = groups[rarity] or {}
    table.insert(groups[rarity], m.value)
  end
  local function median(list)
    table.sort(list)
    return list[math.ceil(#list / 2)]
  end
  print(string.format("%-12s %-3s %5s  %6s %6s %6s %6s  %6s  %s", "coin", "rar", "cost", "pts", "gold", "pen", "nrg", "value", "vs rarity median"))
  table.sort(rows, function(a, b) return a.m.value > b.m.value end)
  for _, r in ipairs(rows) do
    local med = median(groups[r.rarity])
    local ratio = med ~= 0 and r.m.value / med or 0
    local flag = ratio > 1.6 and "  <-- strong" or ratio < .4 and "  <-- weak" or ""
    print(string.format("%-12s %-3s %5d  %6.2f %6.2f %6.2f %6.2f  %6.2f  %5.2fx%s",
      r.id, r.rarity, r.cost, r.m.points, r.m.gold, r.m.penalty, r.m.energy, r.m.value, ratio, flag))
  end  print("note: value = pts - penalty + 0.5*gold + 0.3*energy per flip of that coin. It ignores extra draws, odds boosts"
    .. " and discard synergies, so Lucky, Focus and Fuse read low.")
end

-- ---------------------------------------------------------------- bots
local function deck_mean(g)
  local sum = 0
  for _, c in ipairs(g.coins) do sum = sum + coin_value(c.id) end
  return sum / math.max(1, #g.coins)
end

-- Flip the dealt coin; one we cannot pay for is discarded instead (discarding is free).
local function finish_flip(g)
  if not Game.can_flip(g) then
    if Game.discard(g) > 0 then return end
  end
  if Game.flip(g) then Game.resolve(g) end
end

local function have_item(g, id)
  for slot, owned in ipairs(g.items) do if owned == id then return slot end end
end

local bots = {}

-- Floor: flips whatever is dealt and buys random affordable things.
local function keep_everything(g) Game.mulligan_done(g) end

bots.random = {
  mulligan = keep_everything,
  encounter = function(g) finish_flip(g) end,
  shop = function(g)
    for _ = 1, 8 do
      local pick = (g.rng_state % 4) + 1
      if not Game.buy(g, pick) then break end
    end
  end,
}

-- Buys the most expensive coin it can afford until the bank is full; never discards or uses items.
bots.greedy = {
  mulligan = keep_everything,
  encounter = function(g) finish_flip(g) end,
  shop = function(g)
    while true do
      if #g.coins >= g.slots and not Game.buy_slot(g) then break end -- a full deck: buy a slot first
      local best, best_cost = nil, -1
      for index, id in ipairs(g.shop_offers) do
        local cost = id and (catalog[id].cost or 15)
        if id and g.player.gold >= cost and cost > best_cost then best, best_cost = index, cost end
      end
      if not best or not Game.buy(g, best) then break end
    end
  end,
}

-- Uses measured coin values: discards weak coins, plays items when behind pace, buys the best coins.
bots.smart = {
  keep_playing = true, -- extra points pay gold, so it flips until the stack runs out
  -- discard the weak coins of the opening hand (free) while at least three stay
  mulligan = function(g)
    local hand = g.mulligan.hand
    local sum = 0
    for _, uid in ipairs(hand) do sum = sum + coin_value(Game.get_coin(g, uid).id) end
    local mean = sum / #hand
    local marked = {}
    for _, uid in ipairs(hand) do
      if #hand - #marked > 3 and coin_value(Game.get_coin(g, uid).id) < mean * .6 then marked[#marked + 1] = uid end
    end
    Game.mulligan_discard(g, marked)
    Game.mulligan_done(g)
  end,

  encounter = function(g)
    local e = g.encounter
    local coin = Game.get_coin(g, g.dealt.uid)
    local def = catalog[coin.id]
    local v, mean = coin_value(coin.id), deck_mean(g)
    local behind = e.quota > Game.coins_left(g) * mean * .9

    if v < mean * .6 and #g.coins - e.discards > 1 and Game.discard(g) > 0 then return end
    local swap = have_item(g, "swap")
    if swap and v < mean * .5 and Game.use_item(g, swap) then return end

    local function heads_points()
      local sum = 0
      for _, effect in ipairs(def.heads) do if effect.type == "score" then sum = sum + effect.amount end end
      return sum
    end
    if behind then
      local slot = have_item(g, "double_down")
      if slot and heads_points() >= 3 then Game.use_item(g, slot) end
      slot = have_item(g, "force_heads")
      if slot and heads_points() >= 2 then Game.use_item(g, slot) end
      slot = have_item(g, "weighted")
      if slot and g.dealt and g.dealt.probability < .75 then Game.use_item(g, slot) end
    end
    local extra = have_item(g, "extra_draw")
    if extra and Game.coins_left(g) <= 2 and e.quota > 0 and e.quota <= 2 * mean then Game.use_item(g, extra) end
    finish_flip(g)
  end,

  shop = function(g)
    local function worst_coin()
      local worst, worst_value = nil, math.huge
      for _, c in ipairs(g.coins) do
        local v = coin_value(c.id)
        if v < worst_value then worst, worst_value = c, v end
      end
      return worst, worst_value
    end
    -- coins: best value per offer, replacing the worst coin once the bank is full
    local improved = true
    while improved do
      improved = false
      local best, best_value = nil, -math.huge
      for index, id in ipairs(g.shop_offers) do
        if id and g.player.gold >= (catalog[id].cost or 15) and coin_value(id) > best_value then
          best, best_value = index, coin_value(id)
        end
      end
      if best then
        local price = catalog[g.shop_offers[best]].cost or 15
        if #g.coins < g.slots then
          improved = Game.buy(g, best)
        elseif g.slots < Game.DECK_MAX and g.player.gold >= Game.SLOT_COST + price and best_value > .5 then
          improved = Game.buy_slot(g) and Game.buy(g, best) -- a new slot is cheaper than dropping a coin
        else
          local worst, worst_value = worst_coin()
          -- a full deck must lose a coin first (removal costs 8 gold)
          if best_value > worst_value * 1.3 + .1 and g.player.gold >= 8 + price and Game.remove(g, worst.uid) then
            improved = Game.buy(g, best)
          end
        end
      end
    end
    -- relic
    if g.shop_relic and g.player.gold >= 25 then Game.buy_relic(g) end
    -- items worth carrying
    for index, id in ipairs(g.shop_items) do
      if id and (id == "force_heads" or id == "double_down" or id == "weighted" or id == "extra_draw")
        and #g.items < Items.MAX and g.player.gold >= item_catalog[id].cost then
        Game.buy_item(g, index)
      end
    end
    -- permanent odds upgrade on the best coin with whatever is left
    local best_coin, best_value = nil, -math.huge
    for _, c in ipairs(g.coins) do
      if coin_value(c.id) > best_value then best_coin, best_value = c, coin_value(c.id) end
    end
    while best_coin and g.player.gold >= 10 and Game.upgrade(g, best_coin.uid) do end
  end,
}

-- ---------------------------------------------------------------- running
local function unlocked_for(character)
  if opts.unlock ~= "all" then return nil end
  local list = {}
  for _, entry in ipairs(characters[character].locked or {}) do list[#list + 1] = entry[1] end
  return list
end

-- A competent player's coin set: the best available coins (max copies each), Normal fills the rest.
local function best_set(character)
  local unlocked = unlocked_for(character) or {}
  local ids, seen = {}, {}
  for _, id in ipairs(characters[character].pool) do ids[#ids + 1] = id seen[id] = true end
  for _, id in ipairs(unlocked) do if not seen[id] then ids[#ids + 1] = id end end
  table.sort(ids, function(a, b) return coin_value(a) > coin_value(b) end)
  local set = {}
  for _, id in ipairs(ids) do
    if id ~= "normal" and coin_value(id) > coin_value("normal") then
      for _ = 1, Game.MAX_COPIES do if #set < Game.START_MAX then set[#set + 1] = id end end
    end
  end
  while #set < Game.START_MAX do set[#set + 1] = "normal" end
  return set
end

local function play(seed, character, bot)
  local loadout = opts.set ~= "default" and best_set(character) or nil
  local g = Game.new(seed, character, unlocked_for(character), loadout, true)
  local guard = 0
  while g.phase ~= "VICTORY" and g.phase ~= "GAME_OVER" and guard < 2000 do
    guard = guard + 1
    if g.mulligan then bot.mulligan(g)
    elseif g.phase == "ENCOUNTER" then
      if g.dealt then bot.encounter(g)
      elseif g.encounter.cleared then Game.end_level(g)
      elseif Game.can_exchange(g) then Game.exchange(g) end -- every bot exchanges rather than giving up
      -- once the quota is met a bot either keeps flipping for gold or opens the shop at once
      if g.phase == "ENCOUNTER" and g.encounter.cleared and not bot.keep_playing then Game.end_level(g) end
    elseif g.phase == "SHOP" then
      bot.shop(g)
      Game.leave_shop(g)
    end
  end
  return g
end

local function summarize(character, bot_name)
  local bot = bots[bot_name]
  local reached, wins, gold, tokens = {0, 0, 0, 0, 0}, 0, 0, 0
  for seed = 1, opts.runs do
    local g = play(seed, character, bot)
    for level = 0, g.cleared do reached[level + 1] = reached[level + 1] + 1 end
    if g.phase == "VICTORY" then wins = wins + 1 end
    gold = gold + g.player.gold
    tokens = tokens + Game.run_tokens(g)
  end
  local function pct(n) return string.format("%5.1f%%", 100 * n / opts.runs) end
  print(string.format("%-8s %-7s clear L1 %s  L2 %s  L3 %s  boss %s   avg gold %5.1f  avg tokens %4.2f",
    character, bot_name, pct(reached[2]), pct(reached[3]), pct(reached[4]), pct(wins), gold / opts.runs, tokens / opts.runs))
end

if opts.coins then
  coin_report()
  return
end

if opts.trace then
  local g = play(opts.trace, opts.char == "all" and "blade" or opts.char, bots[opts.bot == "all" and "smart" or opts.bot])
  print(table.concat(g.log, "\n"))
  local deck = {}
  for _, c in ipairs(g.coins) do deck[#deck + 1] = c.id end
  print("END: " .. g.phase .. ", cleared " .. g.cleared .. ", deck " .. table.concat(deck, ", "))
  return
end

local route = {}
for level, stage in ipairs(Game.route) do route[#route + 1] = stage.per_coin .. "/" .. (stage.payout or "-") end
print("route (quota per coin/payout): " .. table.concat(route, "  ") .. "   runs per row: " .. opts.runs ..
  (opts.unlock == "all" and "   all locked coins unlocked" or ""))
local names = {"blade", "seer", "trader"}
local bot_names = {"random", "greedy", "smart"}
for _, character in ipairs(names) do
  if opts.char == "all" or opts.char == character then
    for _, bot_name in ipairs(bot_names) do
      if opts.bot == "all" or opts.bot == bot_name then summarize(character, bot_name) end
    end
  end
end
