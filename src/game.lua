local RNG = require("src.rng")
local Signal = require("src.signal")
local Hooks = require("src.hooks")
local Relics = require("src.relics")
local Items = require("src.items")
local catalog = require("content.coins")
local relic_catalog = require("content.relics")
local item_catalog = require("content.items")
local characters = require("content.characters")
local Game = {}

Game.VISIBLE = 3 -- coins shown in the bank; the first one is the coin you are about to play
Game.MULLIGAN = 5 -- coins drawn at the start of a level, from which you may discard
Game.START_MAX = 10 -- coins in a coin set you can take into a run
Game.DECK_MAX = 10 -- the shop can never grow the deck past this (a full deck replaces a coin)
Game.SURPLUS_RATE = .5 -- gold per point scored beyond the quota (rounded down in total)
Game.MAX_COPIES = 3 -- copies of one coin in a set; the plain Normal coin is exempt (up to the set size)

local route = {
  {name = "Opening", quota = 3, flips = 12, payout = 20},
  {name = "Second Chance", quota = 11, flips = 10, payout = 25},
  {name = "High Stakes", quota = 20, flips = 10, payout = 30},
  {name = "The House", quota = 36, flips = 12, boss = true},
}

local function log(game, message)
  game.log[#game.log + 1] = message
end

local function coin(game, id)
  assert(catalog[id], "unknown coin: " .. tostring(id))
  game.next_uid = game.next_uid + 1
  return {uid = game.next_uid, id = id, bonus = 0}
end

local function find_coin(game, uid)
  for _, item in ipairs(game.coins) do
    if item.uid == uid then return item end
  end
end

local function active_count(game)
  return #game.coins
end

local function add_to_deck(game, id)
  local item = coin(game, id)
  if #game.coins >= Game.DECK_MAX then
    for index, owned in ipairs(game.coins) do
      if owned.uid == game.selected_uid then
        table.remove(game.coins, index)
        break
      end
    end
  end
  game.coins[#game.coins + 1] = item
  game.selected_uid = item.uid
  return item
end

local function offers(game, pool, count)
  local choices = {}
  local remaining = {}
  for _, id in ipairs(pool) do remaining[#remaining + 1] = id end
  for _ = 1, math.min(count, #remaining) do
    local index = RNG.int(game, 1, #remaining)
    choices[#choices + 1] = table.remove(remaining, index)
  end
  return choices
end

-- Coins the shop may sell: the character's base pool plus anything unlocked with tokens.
local function shop_pool(game)
  local pool, seen = {}, {}
  for _, id in ipairs(characters[game.character_id].pool) do pool[#pool + 1] = id seen[id] = true end
  for _, id in ipairs(game.unlocked) do
    if not seen[id] then pool[#pool + 1] = id seen[id] = true end
  end
  return pool
end

-- Four coin offers; slot 1 is always the cheap Normal coin so the bank can always be topped up.
local function shop_stock(game)
  local others = {}
  for _, id in ipairs(shop_pool(game)) do
    if id ~= "normal" then others[#others + 1] = id end
  end
  local stock = {"normal"}
  for _, id in ipairs(offers(game, others, 3)) do stock[#stock + 1] = id end
  return stock
end

-- Draw pile: every owned coin that is not discarded this level and not already waiting in the bank.
local function shuffle_deck(game)
  local pile = {}
  local e = game.encounter
  local out = e and e.discarded or {}
  local held = {}
  for _, uid in ipairs(e and e.queue or {}) do held[uid] = true end
  for _, uid in ipairs(game.mulligan and game.mulligan.hand or {}) do held[uid] = true end
  if game.pending then held[game.pending.uid] = true end -- still being flipped
  for _, item in ipairs(game.coins) do
    if not out[item.uid] and not held[item.uid] then pile[#pile + 1] = item.uid end
  end
  for i = #pile, 2, -1 do
    local j = RNG.int(game, 1, i)
    pile[i], pile[j] = pile[j], pile[i]
  end
  return pile
end

function Game.probability(game, item)
  local bonus = game.phase == "ENCOUNTER" and game.encounter and game.encounter.bonus[item.uid] or 0
  local magnet = game.phase == "ENCOUNTER" and game.encounter and game.encounter.magnet or 0
  local p = catalog[item.id].probability + item.bonus + bonus + magnet
  return math.max(0, math.min(1, Hooks.odds(game, item, p)))
end

local deal

-- Top the bank up to VISIBLE coins from the draw pile (reshuffling when it runs dry).
local function refill(game)
  local e = game.encounter
  while #e.queue < Game.VISIBLE do
    if #e.pile == 0 then e.pile = shuffle_deck(game) end
    if #e.pile == 0 then return end
    e.queue[#e.queue + 1] = table.remove(e.pile, 1)
  end
end

local function start_encounter(game)
  local stage = route[game.encounter_index]
  game.encounter = nil -- discards from the previous level must not carry over
  game.encounter = {name = stage.name, quota = stage.quota, max_quota = stage.quota,
    draws = stage.flips, max_draws = stage.flips, boss = stage.boss or false, payout = stage.payout,
    flips = 0, scored = 0, cleared = false, surplus_paid = 0, discarded = {}, discards = 0, bonus = {}, magnet = 0, streak = 0, bonus_draws = 0, pile = shuffle_deck(game), queue = {}}
  game.player.energy = game.player.max_energy
  game.pending = nil
  game.last_result = nil
  game.phase = "ENCOUNTER"
  log(game, "Encounter " .. game.encounter_index .. ": " .. stage.name .. " (quota " .. stage.quota .. ")")
  for _, owned in ipairs(game.coins) do Hooks.grow(owned, "level") end
  Signal.emit("encounter_start", {game = game, encounter = game.encounter})
  -- mulligan: draw a hand to look at; the UI lets the player discard before play starts
  local hand = {}
  for _ = 1, math.min(Game.MULLIGAN, #game.encounter.pile) do
    hand[#hand + 1] = table.remove(game.encounter.pile, 1)
  end
  game.mulligan = {hand = hand}
  game.dealt = nil
  if not game.manual_mulligan then Game.mulligan_done(game) end
end

-- Deal the coin at the front of the bank; the player may discard it or flip it.
function deal(game)
  local e = game.encounter
  refill(game)
  local uid = e.queue[1]
  local inst = find_coin(game, uid)
  game.dealt = {uid = uid}
  game.selected_uid = uid
  log(game, catalog[inst.id].name .. " #" .. uid .. " dealt.")
  game.peek = nil
  Hooks.bind(game, inst)
  Signal.emit("coin_deal", {game = game, inst = inst})
  game.dealt.probability = Game.probability(game, inst) -- on_deal may have changed the odds
end

-- Discard coins from the opening hand (free). They stay out of play for the level, and at least
-- one coin must be kept. uids is a list of coin uids; returns how many were discarded.
function Game.mulligan_discard(game, uids)
  local m, e = game.mulligan, game.encounter
  if not m then return 0 end
  local count = 0
  for _, uid in ipairs(uids) do
    for index, held in ipairs(m.hand) do
      if held == uid and #m.hand > 1 then
        table.remove(m.hand, index)
        e.discarded[uid] = true
        e.discards = e.discards + 1
        log(game, catalog[find_coin(game, uid).id].name .. " #" .. uid .. " discarded from the opening hand.")
        count = count + 1
        break
      end
    end
  end
  return count
end

-- Keep the remaining hand as the bank and start the level.
function Game.mulligan_done(game)
  local m = game.mulligan
  if not m then return false end
  game.encounter.queue = m.hand
  game.mulligan = nil
  deal(game)
  return true
end

-- unlocked: optional list of extra coin ids bought with tokens in the main menu.
-- loadout: optional list of coin ids to start with (at most START_MAX, from the character's
-- pool plus unlocked coins); defaults to the character's deck.
-- manual_mulligan: the UI sets this and calls Game.mulligan_done itself.
function Game.new(seed, character_id, unlocked, loadout, manual_mulligan)
  character_id = character_id or "blade"
  assert(characters[character_id], "unknown character: " .. tostring(character_id))
  local normalized = RNG.seed(seed)
  local game = {seed = normalized, rng_state = normalized, last_rng = nil,
    character_id = character_id,
    phase = "ENCOUNTER", player = {gold = 10, energy = 3, max_energy = 3},
    coins = {}, relics = {}, items = {}, shop_items = {}, unlocked = unlocked or {}, cleared = 0, shop_relic = nil, next_uid = 0, encounter_index = 1, encounter = nil,
    pending = nil, shop_offers = {}, log = {}, selected_uid = nil}
  local def = characters[character_id]
  game.manual_mulligan = manual_mulligan
  if loadout then
    assert(#loadout >= 1 and #loadout <= Game.START_MAX, "loadout must have 1-" .. Game.START_MAX .. " coins")
    local allowed = {}
    for _, id in ipairs(shop_pool(game)) do allowed[id] = true end
    local copies = {}
    for _, id in ipairs(loadout) do
      assert(allowed[id], "coin not available to this character: " .. tostring(id))
      copies[id] = (copies[id] or 0) + 1
      assert(id == "normal" or copies[id] <= Game.MAX_COPIES, "too many copies of " .. id)
    end
  end
  for i, id in ipairs(loadout or def.deck or {def.starter}) do game.coins[i] = coin(game, id) end
  game.selected_uid = game.coins[1].uid
  log(game, "Seed: " .. normalized)
  Relics.bind(game)
  Items.clear()
  start_encounter(game)
  return game
end

function Game.select(game, uid)
  if find_coin(game, uid) then game.selected_uid = uid return true end
  return false
end

-- Roll the dice for a pending flip, then let on_flip hooks change the outcome.
-- The side that will count is decided here, before the UI animates it: relics may change the
-- outcome, then the boss inverts every 5th flip (an encounter rule, so it comes last).
local function finalize(game, item, flip)
  local e = game.encounter
  local nth = e.flips + 1
  local before = flip.result
  local outcome = {game = game, inst = item, flips = nth, result = before}
  Signal.emit("coin_outcome", outcome)
  local final = outcome.result
  flip.altered = final ~= before and "RELIC" or nil -- shown in the UI so a changed side is never a mystery
  if e.boss and nth % 5 == 0 then
    final = final == "Heads" and "Tails" or "Heads"
    flip.altered = (flip.altered and flip.altered .. " + " or "") .. "THE HOUSE"
  end
  flip.result = final
end

local function roll(game, item, flip)
  flip.raw = RNG.random(game) < flip.probability and "Heads" or "Tails"
  flip.result = flip.raw
  flip.forced = nil
  flip.altered = nil
  Signal.emit("coin_flip", {game = game, inst = item, flip = flip})
  finalize(game, item, flip)
end

function Game.flip(game)
  if game.phase ~= "ENCOUNTER" or game.pending or not game.dealt then return false end
  if not Game.can_flip(game) then return false end
  local uid, probability = game.dealt.uid, game.dealt.probability
  local item = find_coin(game, uid)
  game.player.energy = game.player.energy - math.min(Game.flip_cost(game, uid), game.player.energy)
  game.pending = {uid = uid, probability = probability}
  game.dealt = nil
  table.remove(game.encounter.queue, 1) -- a flipped coin leaves the bank at once
  refill(game)
  roll(game, item, game.pending)
  log(game, catalog[item.id].name .. " #" .. uid .. " rolled " .. game.pending.raw ..
    (game.pending.result ~= game.pending.raw and " → " .. game.pending.result or "") .. ".")
  return true
end

-- Energy a coin costs to flip (0 for most coins).
function Game.flip_cost(game, uid)
  return catalog[find_coin(game, uid).id].energy_cost or 0
end

-- Can the dealt coin be flipped right now? A coin you cannot pay for must be discarded (free) --
-- unless it is the last usable coin, which always flips so a level can never dead-end.
function Game.can_flip(game)
  if game.phase ~= "ENCOUNTER" or game.pending or not game.dealt then return false end
  local e = game.encounter
  return Game.flip_cost(game, game.dealt.uid) <= game.player.energy or #game.coins - e.discards <= 1
end

-- Discard coins from the bank for the rest of the level. Free. uids is a list of bank coins; with
-- no list the front coin goes. At least one coin must stay usable. Returns how many were discarded.
function Game.discard(game, uids)
  local e = game.encounter
  if game.phase ~= "ENCOUNTER" or game.pending or game.mulligan or not game.dealt then return 0 end
  local in_bank = {}
  for _, uid in ipairs(e.queue) do in_bank[uid] = true end
  local targets, seen = {}, {}
  for _, uid in ipairs(uids or {game.dealt.uid}) do
    if in_bank[uid] and not seen[uid] then targets[#targets + 1] = uid seen[uid] = true end
  end
  if #targets == 0 or #game.coins - e.discards - #targets < 1 then return 0 end
  local front = game.dealt.uid
  Hooks.unbind()
  for _, uid in ipairs(targets) do
    for index, queued in ipairs(e.queue) do
      if queued == uid then table.remove(e.queue, index) break end
    end
    e.discarded[uid] = true
    e.discards = e.discards + 1
    local inst = find_coin(game, uid)
    log(game, catalog[inst.id].name .. " #" .. uid .. " discarded for this level.")
    Hooks.bind(game, inst) -- so the coin's own on_discard hook runs even if it was not the front coin
    Signal.emit("coin_discard", {game = game, inst = inst})
    Hooks.unbind()
    Hooks.grow(inst, "discard")
  end
  if seen[front] then
    deal(game)
  else
    Hooks.bind(game, find_coin(game, front)) -- the dealt coin stays dealt; restore its hooks
  end
  return #targets
end

function Game.reroll(game)
  local result = game.phase == "ENCOUNTER" and game.pending
  if not result or game.player.energy < 1 then return false end
  game.player.energy = game.player.energy - 1
  roll(game, find_coin(game, result.uid), result)
  log(game, catalog[find_coin(game, result.uid).id].name .. " rerolled: " .. result.result)
  return true
end

function Game.force(game, side)
  local result = game.phase == "ENCOUNTER" and game.pending
  if not result or game.player.energy < 2 or (side ~= "Heads" and side ~= "Tails") then return false end
  game.player.energy = game.player.energy - 2
  result.result = side
  result.forced = true
  finalize(game, find_coin(game, result.uid), result)
  log(game, catalog[find_coin(game, result.uid).id].name .. " forced to " .. side)
  return true
end

local function apply_effect(game, item, effect)
  local p, e = game.player, game.encounter
  if effect.type == "gold" then
    p.gold = p.gold + effect.amount
    return "+" .. effect.amount .. " gold"
  elseif effect.type == "score" then
    e.quota = math.max(0, e.quota - effect.amount) -- quota is what is still missing
    e.scored = e.scored + effect.amount -- points earned this level (may overshoot on the last flip)
    return "+" .. effect.amount .. " points"
  elseif effect.type == "energy" then
    p.energy = p.energy + effect.amount
    return "+" .. effect.amount .. " energy"
  elseif effect.type == "penalty" then
    e.quota = e.quota + effect.amount -- a penalty moves the goalposts
    e.max_quota = e.max_quota + effect.amount
    return "quota +" .. effect.amount
  elseif effect.type == "extra_draw" then
    local gained = math.min(effect.amount, 3 - e.bonus_draws)
    e.bonus_draws = e.bonus_draws + gained
    e.draws = e.draws + gained
    return gained > 0 and ("+" .. gained .. " draw") or "extra draw limit reached"
  elseif effect.type == "probability" then
    e.bonus[item.uid] = (e.bonus[item.uid] or 0) + effect.amount
    return "+" .. math.floor(effect.amount * 100 + .5) .. "% Heads this encounter"
  end
  error("unknown effect: " .. tostring(effect.type))
end

-- Stock the shop after a cleared level: four coins and one relic, all from the seeded RNG.
local function enter_shop(game)
  game.phase = "SHOP"
  game.shop_offers = shop_stock(game)
  game.shop_relic = nil
  local owned = {}
  for _, id in ipairs(game.relics) do owned[id] = true end
  local unowned = {}
  for id in pairs(relic_catalog) do
    if not owned[id] then unowned[#unowned + 1] = id end
  end
  table.sort(unowned) -- stable order so the seeded pick is deterministic
  if #unowned > 0 then game.shop_relic = unowned[RNG.int(game, 1, #unowned)] end
  local item_ids = {}
  for id in pairs(item_catalog) do item_ids[#item_ids + 1] = id end
  table.sort(item_ids)
  game.shop_items = offers(game, item_ids, 2)
end

-- The level is over for good: the player opened the shop (or the boss fell). Only possible once the
-- quota is met. The payout was already given when the quota was met.
function Game.end_level(game)
  local e = game.encounter
  if game.phase ~= "ENCOUNTER" or not e.cleared or game.pending or game.mulligan then return false end
  Hooks.unbind()
  game.dealt = nil
  Items.clear()
  Signal.emit("encounter_end", {game = game, won = true})
  if e.boss then game.phase = "VICTORY" else enter_shop(game) end
  return true
end

function Game.resolve(game)
  if game.phase ~= "ENCOUNTER" or not game.pending then return false end
  local e = game.encounter
  e.draws = e.draws - 1
  local result = game.pending
  local item = find_coin(game, result.uid)
  e.flips = e.flips + 1
  local final = result.result -- already decided (relics, boss inversion) when the coin was flipped
  result.final = final
  e.streak = final == "Heads" and e.streak + 1 or 0
  -- hooks get a private copy of the effect list so they can edit it without touching the def
  local res = {result = final, raw = result.raw, effects = {}}
  for i, effect in ipairs(catalog[item.id][string.lower(final)]) do
    res.effects[i] = {type = effect.type, amount = effect.amount}
  end
  Signal.emit("coin_resolve", {game = game, inst = item, res = res})
  local messages = {}
  local scored_before, quota_total_before = e.scored, e.max_quota
  for _, effect in ipairs(res.effects) do
    local text = apply_effect(game, item, effect)
    messages[#messages + 1] = text
    Signal.emit("effect_applied", {game = game, inst = item, effect = effect, text = text})
  end
  result.gained = e.scored - scored_before -- for the UI: what this flip was worth
  result.penalty = e.max_quota - quota_total_before
  Signal.emit("coin_resolved", {game = game, inst = item, res = res})
  Hooks.unbind()
  Hooks.grow(item, "flip")
  log(game, catalog[item.id].name .. " #" .. item.uid .. ": " .. final ..
    (final ~= result.raw and " (raw " .. result.raw .. ")" or "") ..
    " → " .. (#messages > 0 and table.concat(messages, ", ") or "nothing"))
  game.last_result = result
  game.pending = nil
  if e.quota <= 0 and not e.cleared then
    e.cleared = true
    game.cleared = game.cleared + 1
    log(game, "Quota met! " .. e.name .. " cleared.")
    if not e.boss then
      game.player.gold = game.player.gold + e.payout
      log(game, "+" .. e.payout .. " gold level payout.")
    end
  end
  if e.cleared then -- keep flipping after the quota: every 1/SURPLUS_RATE extra points pay one gold
    local owed = math.floor(math.max(0, e.scored - e.max_quota) * Game.SURPLUS_RATE)
    if owed > e.surplus_paid then
      game.player.gold = game.player.gold + owed - e.surplus_paid
      log(game, "+" .. (owed - e.surplus_paid) .. " gold for extra points.")
      e.surplus_paid = owed
    end
  end
  if e.cleared and (e.boss or e.draws <= 0) then
    Game.end_level(game) -- the boss ends the run; out of draws sends you to the shop
  elseif e.draws <= 0 then
    game.phase = "GAME_OVER"
    log(game, "Defeat: out of draws.")
    Items.clear()
    Signal.emit("encounter_end", {game = game, won = false})
  else
    deal(game)
  end
  return true
end

-- Give the player a relic and (re)bind every owned relic to the event bus.
function Game.add_relic(game, id)
  game.relics[#game.relics + 1] = id
  Relics.bind(game)
end

function Game.buy_item(game, index)
  local id = game.phase == "SHOP" and game.shop_items[index]
  if not id or #game.items >= Items.MAX or game.player.gold < item_catalog[id].cost then return false end
  game.shop_items[index] = false
  game.player.gold = game.player.gold - item_catalog[id].cost
  game.items[#game.items + 1] = id
  log(game, "Bought " .. item_catalog[id].name .. " for " .. item_catalog[id].cost .. " gold.")
  return true
end

function Game.use_item(game, slot) return Items.use(game, slot) end

function Game.buy_relic(game)
  if game.phase ~= "SHOP" or not game.shop_relic or game.player.gold < 25 then return false end
  game.player.gold = game.player.gold - 25
  Game.add_relic(game, game.shop_relic)
  log(game, "Bought relic " .. relic_catalog[game.shop_relic].name .. " for 25 gold.")
  game.shop_relic = nil
  return true
end

function Game.buy(game, index)
  local id = game.phase == "SHOP" and game.shop_offers[index]
  if not id or game.player.gold < (catalog[id].cost or 15) then return false end
  game.shop_offers[index] = false
  game.player.gold = game.player.gold - (catalog[id].cost or 15)
  add_to_deck(game, id)
  log(game, "Bought " .. catalog[id].name .. " for " .. (catalog[id].cost or 15) .. " gold.")
  return true
end

function Game.reroll_shop(game)
  if game.phase ~= "SHOP" or game.player.gold < 4 then return false end
  game.player.gold = game.player.gold - 4
  game.shop_offers = shop_stock(game)
  log(game, "Shop rerolled for 4 gold.")
  return true
end

function Game.buy_energy(game)
  if game.phase ~= "SHOP" or game.player.gold < 20 or game.player.max_energy >= 5 then return false end
  game.player.gold = game.player.gold - 20
  game.player.max_energy = game.player.max_energy + 1
  log(game, "Bought +1 maximum energy for 20 gold.")
  return true
end

function Game.remove(game, uid)
  if game.phase ~= "SHOP" or game.player.gold < 8 or #game.coins <= 1 then return false end
  for index, item in ipairs(game.coins) do
    if item.uid == uid then
      table.remove(game.coins, index)
      game.player.gold = game.player.gold - 8
      game.selected_uid = game.coins[1].uid
      log(game, "Removed " .. catalog[item.id].name .. " for 8 gold.")
      return true
    end
  end
  return false
end

function Game.upgrade(game, uid)
  if game.phase ~= "SHOP" or game.player.gold < 10 then return false end
  local item = find_coin(game, uid)
  if not item or Game.probability(game, item) >= 1 then return false end
  game.player.gold = game.player.gold - 10
  item.bonus = math.min(1 - catalog[item.id].probability, item.bonus + .10)
  log(game, catalog[item.id].name .. " upgraded to " .. math.floor((catalog[item.id].probability + item.bonus) * 100 + .5) .. "% Heads.")
  return true
end

function Game.leave_shop(game)
  if game.phase ~= "SHOP" then return false end
  return Game.next_encounter(game)
end

function Game.next_encounter(game)
  if game.phase ~= "SHOP" or active_count(game) == 0 then return false end
  game.encounter_index = game.encounter_index + 1
  start_encounter(game)
  return true
end

-- Meta currency earned by a run: 1 per level cleared, +3 for beating the boss.
function Game.run_tokens(game)
  return game.cleared + (game.phase == "VICTORY" and 3 or 0)
end

Game.route = route -- level quotas, draws and payouts; tools/sim.lua overrides them for balance sweeps
Game.apply_effect = apply_effect -- for hooks: apply an effect table to the running game
Game.log = log
function Game.catalog() return catalog end
function Game.relics() return relic_catalog end
function Game.item_catalog() return item_catalog end
function Game.characters() return characters end
function Game.active_count(game) return active_count(game) end
function Game.get_coin(game, uid) return find_coin(game, uid) end

return Game
