local RNG = require("src.rng")
local Signal = require("src.signal")
local Hooks = require("src.hooks")
local Relics = require("src.relics")
local Items = require("src.items")
local catalog = require("content.coins")
local relic_catalog = require("content.relics")
local item_catalog = require("content.items")
local modifier_catalog = require("content.modifiers")
local stake_catalog = require("content.stakes")
local MODIFIER_ORDER = {"lucky_day", "cold_snap", "power_surge", "blackout", "gold_rush", "high_stakes", "good_rhythm", "bonus_exchange"}
local characters = require("content.characters")
local Profile = require("src.profile")
local Game = {}

Game.DEV_MODE = os.getenv("TOSSUP_DEV") == "1"
Game.SANDBOX_MODE = os.getenv("SANDBOX") == "1"
Game.VISIBLE = 3 -- coins shown in the bank; the first one is the coin you are about to play
Game.MULLIGAN = 5 -- coins drawn at the start of a level, from which you may discard
Game.START_MAX = 5 -- coins in a coin set = the deck slots a run starts with
Game.DECK_MAX = 10 -- the most deck slots; the shop sells the extra ones one by one
Game.SLOT_COST, Game.SLOT_STEP = 5, 2 -- gold for the first extra deck slot, and how much more every further one costs
Game.EXCHANGE_BASE = 10 -- gold for the first exchange of a level (empty stack): played coins come back
Game.EXCHANGE_STEP = 5 -- every further exchange in the same level costs this much more
Game.EXCHANGE_GAIN = 3 -- played coins that come back into the stack in exchange
Game.COMBO_STEP, Game.COMBO_CAP = 0.25, 3 -- combo: x1 + 0.25 per extra same result in a row, up to x3
Game.EXCHANGE_MAX = 3 -- exchanges per level; after the third one an empty stack loses the level
Game.RETURN_CAP = 3 -- "extra draw" effects (a coin returning to the pile) per level
Game.START_GOLD = 5
Game.SURPLUS_RATE = .5 -- gold per point scored beyond the quota (rounded down in total)

local route = {
  {name = "Opening", per_coin = 0.7, payout = 25},
  {name = "Second Chance", per_coin = 1.4, payout = 30},
  {name = "High Stakes", per_coin = 2.5, payout = 35},
  {name = "The House", per_coin = 4.5, boss = true},
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

-- Coins a coin set may contain: the character's starting pool plus coins already unlocked.
local function usable_pool(game)
  local pool, seen = {}, {}
  for _, id in ipairs(characters[game.character_id].pool) do pool[#pool + 1] = id seen[id] = true end
  for _, id in ipairs(game.unlocked) do
    if not seen[id] then pool[#pool + 1] = id seen[id] = true end
  end
  return pool
end

-- Every coin the shop may offer this character, including coins that are still locked: buying a
-- locked coin in the shop is what unlocks it (see game.purchased).
local function shop_pool(game)
  if game.sandbox then
    local pool, seen = {}, {}
    for _, id in ipairs(game.sandbox.coins) do
      if not seen[id] then pool[#pool + 1] = id seen[id] = true end
    end
    return pool
  end
  local pool = usable_pool(game)
  local seen = {}
  for _, id in ipairs(pool) do seen[id] = true end
  for _, entry in ipairs(characters[game.character_id].locked or {}) do
    if not seen[entry[1]] then pool[#pool + 1] = entry[1] seen[entry[1]] = true end
  end
  return pool
end

-- Four distinct coin offers from the whole coin list.
local function shop_stock(game)
  return offers(game, shop_pool(game), 4)
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
  local boost = 0
  if game.phase == "ENCOUNTER" and game.encounter then
    for _, buff in ipairs(game.encounter.buffs) do
      if buff.kind == "odds" and not buff.fresh then boost = boost + buff.amount end
    end
  end
  local override = game.sandbox and game.sandbox.odds and game.sandbox.odds[item.id]
  local p = (override and override.heads or catalog[item.id].probability) + item.bonus + bonus + magnet + boost
  local max_heads = 1 - Game.tie_probability(game, item)
  if game.phase ~= "ENCOUNTER" then return math.max(0, math.min(max_heads, p)) end -- odds hooks read the level: none in the shop
  return math.max(0, math.min(max_heads, Hooks.odds(game, item, p)))
end

function Game.tie_probability(game, item)
  local override = game.sandbox and game.sandbox.odds and game.sandbox.odds[item.id]
  return override and override.tie or catalog[item.id].tie_probability or 0
end

-- Only numeric, even-valued effects currently have an Edge outcome. Halving both sides is exact.
function Game.tie_effects(id)
  local def = catalog[id]
  local effects = {}
  for _, side in ipairs({"tails", "heads"}) do -- penalties first, so score is not clamped before quota rises
    for _, effect in ipairs(def[side]) do
      assert((effect.type == "score" or effect.type == "gold" or effect.type == "energy" or effect.type == "penalty")
        and effect.amount % 2 == 0, "Edge requires even numeric effects: " .. id)
      effects[#effects + 1] = {type = effect.type, amount = effect.amount / 2}
    end
  end
  return effects
end

local deal
-- Top the bank up to VISIBLE coins from the draw pile. The pile is never reshuffled: a level lasts
-- exactly as long as the coins in your stack (bank + pile).
local function refill(game)
  local e = game.encounter
  while #e.queue < Game.VISIBLE do
    if #e.pile == 0 and game.reshuffle then e.pile = shuffle_deck(game) end -- tests / coin simulator only
    if #e.pile == 0 then return end
    e.queue[#e.queue + 1] = table.remove(e.pile, 1)
  end
end

function Game.coins_left(game)
  local e = game.encounter
  return #e.queue + #e.pile
end

-- A level's quota scales with the size of your deck (points per coin), because a level lasts exactly as
-- long as your stack: a bigger deck means more flips.
-- A level of the run. Beyond the route (after the boss) the levels are endless: each asks 0.5 more points per
-- coin than the one before, pays more, and inverts every 5th flip like The House.
function Game.stage(level)
  if route[level] then return route[level] end
  local k = level - #route
  return {name = "Endless", endless = k, per_coin = route[#route].per_coin + 0.5 * k, payout = 40 + 5 * k, inverts = true}
end

function Game.quota_for(level, coin_count, mult)
  return math.max(1, math.floor(Game.stage(level).per_coin * coin_count * (mult or 1) + .5))
end

-- The stage (difficulty) of a run: the value of a rule, from the highest stage that sets it (see content/stakes.lua).
function Game.rule(game, key, default)
  local value = default
  for i = 1, math.min(game.stake or 1, #stake_catalog) do
    local rules = stake_catalog[i].rules
    if rules and rules[key] ~= nil then value = rules[key] end
  end
  return value
end

-- Shop price of a coin, chip or prize after the stage's price rule.
function Game.price(game, base)
  return math.floor(base * Game.rule(game, "price_mult", 1) + .5)
end

local function start_encounter(game)
  local stage = Game.stage(game.encounter_index)
  local quota = Game.quota_for(game.encounter_index, #game.coins, Game.rule(game, "quota_mult", 1))
  game.encounter = nil -- discards from the previous level must not carry over
  game.encounter = {name = stage.name, quota = quota, max_quota = quota,
    boss = stage.boss or false, inverts = stage.boss or stage.inverts or false, endless = stage.endless, payout = stage.payout,
    flips = 0, scored = 0, cleared = false, surplus_paid = 0, discarded = {}, discards = 0, bonus = {}, magnet = 0, streak = 0, buffs = {}, combo_side = nil, combo_len = 0, shield = 0, combo_step = Game.COMBO_STEP, combo_cap = Game.COMBO_CAP,
    returned = 0, played = {}, pile = shuffle_deck(game), queue = {}}
  game.player.energy = game.player.max_energy
  if Game.use_modifiers ~= false and game.encounter_index >= Game.rule(game, "modifiers_from", 2) then -- every level from the second on has a modifier (tests can switch this off)
    local id = MODIFIER_ORDER[RNG.int(game, 1, #MODIFIER_ORDER)]
    game.encounter.modifier = id
    modifier_catalog[id].apply(game, game.encounter)
    quota = game.encounter.quota
  end
  game.pending = nil
  game.last_result = nil
  game.phase = "ENCOUNTER"
  log(game, "Encounter " .. game.encounter_index .. ": " .. stage.name .. " (quota " .. quota .. ")")
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
  if not uid then
    game.dealt = nil
    Hooks.unbind()
    Game.stack_empty(game)
    return
  end
  local inst = find_coin(game, uid)
  game.dealt = {uid = uid}
  game.selected_uid = uid
  log(game, catalog[inst.id].name .. " #" .. uid .. " dealt.")
  game.peek, game.peek_next = game.peek_next, nil
  Hooks.bind(game, inst)
  Signal.emit("coin_deal", {game = game, inst = inst})
  game.dealt.probability = Game.probability(game, inst) -- on_deal may have changed the odds
  game.dealt.tie_probability = Game.tie_probability(game, inst)
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
        local inst = find_coin(game, uid)
        log(game, catalog[inst.id].name .. " #" .. uid .. " discarded from the opening hand.")
        Hooks.bind(game, inst) -- the coin's own on_discard hook runs here too
        Signal.emit("coin_discard", {game = game, inst = inst})
        Hooks.unbind()
        Hooks.grow(inst, "discard")
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

-- unlocked: optional list of extra coin ids the character may sell (tokens are earned and saved, but nothing spends them yet).
-- loadout: optional list of coin ids to start with (at most START_MAX, from the character's
-- pool plus unlocked coins); defaults to the character's deck.
-- manual_mulligan: the UI sets this and calls Game.mulligan_done itself.
function Game.new(seed, character_id, unlocked, loadout, manual_mulligan, stake, sandbox)
  character_id = character_id or "blade"
  assert(characters[character_id], "unknown character: " .. tostring(character_id))
  local normalized = RNG.seed(seed)
  local game = {seed = normalized, rng_state = normalized, last_rng = nil,
    character_id = character_id,
    phase = "ENCOUNTER", player = {gold = Game.START_GOLD, energy = 3, max_energy = 3},
    coins = {}, relics = {}, items = {}, shop_items = {}, unlocked = unlocked or {}, purchased = {}, cleared = 0, shop_relic = nil, next_uid = 0, encounter_index = 1, encounter = nil,
    pending = nil, shop_offers = {}, log = {}, selected_uid = nil, slots = Game.START_MAX, sandbox = sandbox}
  local def = characters[character_id]
  game.stake = math.max(1, math.min(#stake_catalog, stake or 1))
  game.player.gold = Game.DEV_MODE and 5000 or Game.rule(game, "start_gold", Game.START_GOLD)
  game.manual_mulligan = manual_mulligan
  if loadout then
    assert(#loadout >= 1 and #loadout <= Game.START_MAX, "loadout must have 1-" .. Game.START_MAX .. " coins")
    local allowed = {}
    for _, id in ipairs(usable_pool(game)) do allowed[id] = true end
    local counts = {}
    for _, id in ipairs(loadout) do
      assert(allowed[id], "coin not available to this character: " .. tostring(id))
      local rarity = catalog[id].rarity
      counts[rarity] = (counts[rarity] or 0) + 1
      assert(counts[rarity] <= Profile.rarity_limit(id), "too many " .. rarity .. " coins")
    end
  end
  for i, id in ipairs(sandbox and sandbox.coins or loadout or def.deck or {def.starter}) do game.coins[i] = coin(game, id) end
  if sandbox then
    game.slots = math.max(game.slots, #game.coins)
    game.player.gold = sandbox.gold or game.player.gold
    game.player.energy = sandbox.energy or game.player.energy
    game.player.max_energy = game.player.energy
  end
  game.selected_uid = game.coins[1].uid
  log(game, "Seed: " .. normalized)
  Relics.bind(game)
  Items.clear()
  start_encounter(game)
  return game
end

local last_sandbox_seed = 0

function Game.new_sandbox(cfg)
  assert(type(cfg) == "table" and type(cfg.coins) == "table" and #cfg.coins >= 1 and #cfg.coins <= Game.DECK_MAX,
    "sandbox needs 1-" .. Game.DECK_MAX .. " coins")
  for _, id in ipairs(cfg.coins) do assert(catalog[id], "unknown sandbox coin: " .. tostring(id)) end
  for id, odds in pairs(cfg.odds or {}) do
    assert(catalog[id] and type(odds) == "table" and type(odds.heads) == "number" and type(odds.tie) == "number"
      and odds.heads >= 0 and odds.tie >= 0 and odds.heads + odds.tie <= 1, "invalid sandbox odds: " .. tostring(id))
    if odds.tie > 0 then Game.tie_effects(id) end
  end
  local seed = cfg.seed
  if not seed then
    seed = math.max(os.time() + math.floor(os.clock() * 1000000), last_sandbox_seed + 1)
    last_sandbox_seed = seed
  end
  return Game.new(seed, cfg.character or "blade", nil, nil, false, cfg.stake, cfg)
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
  if not outcome.final and final ~= "Tie" and (e.boss or e.inverts) and nth % Game.rule(game, "boss_every", 5) == 0 then
    final = final == "Heads" and "Tails" or "Heads"
    flip.altered = (flip.altered and flip.altered .. " + " or "") .. "THE HOUSE"
  end
  flip.result = final
end

-- Buffs: "the next N coins ..." effects. kind: "mult" (x amount on score and gold), "odds" (+amount Heads),
-- "swap" (use the other side's effects), "heads" (guaranteed Heads). A buff made while a coin resolves is
-- "fresh" and starts with the following coin; every resolved coin uses up one of the remaining coins.
function Game.add_buff(game, kind, amount, coins, immediate)
  local list = game.encounter.buffs
  list[#list + 1] = {kind = kind, amount = amount, left = coins or 1, fresh = not immediate} -- immediate: a chip, active from the coin in play
end

local function buff_active(game, kind)
  local e = game.encounter
  for _, buff in ipairs(e.buffs) do
    if buff.kind == kind and not buff.fresh then return buff end
  end
end

local function tick_buffs(game)
  local list, kept = game.encounter.buffs, {}
  for _, buff in ipairs(list) do
    if buff.fresh then buff.fresh = false
    else buff.left = buff.left - 1 end
    if buff.left > 0 then kept[#kept + 1] = buff end
  end
  game.encounter.buffs = kept
end

local function roll(game, item, flip)
  local value = RNG.random(game)
  flip.raw = value < flip.probability and "Heads" or
    value < flip.probability + (flip.tie_probability or 0) and "Tie" or "Tails"
  flip.result = flip.raw
  flip.forced = nil
  flip.altered = nil
  if buff_active(game, "heads") then flip.result = "Heads" flip.altered = "BUFF" end
  Signal.emit("coin_flip", {game = game, inst = item, flip = flip})
  finalize(game, item, flip)
end

function Game.flip(game)
  if game.phase ~= "ENCOUNTER" or game.pending or not game.dealt then return false end
  if not Game.can_flip(game) then return false end
  local uid, probability = game.dealt.uid, game.dealt.probability
  local item = find_coin(game, uid)
  game.player.energy = game.player.energy - math.min(Game.flip_cost(game, uid), game.player.energy)
  game.pending = {uid = uid, probability = probability, tie_probability = game.dealt.tie_probability}
  game.dealt = nil
  table.remove(game.encounter.queue, 1) -- a flipped coin leaves the bank at once
  game.encounter.played[#game.encounter.played + 1] = uid
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

-- Spend one "bank_discard" (Crystal Ball Tails): throw away any of the visible bank coins, free. Returns true if it worked.
function Game.discard_bank(game, uid)
  local e = game.encounter
  if not e or (e.bank_discards or 0) < 1 then return false end
  local visible = false
  for i = 1, math.min(Game.VISIBLE, #e.queue) do if e.queue[i] == uid then visible = true end end
  if not visible or Game.discard(game, {uid}) == 0 then return false end
  e.bank_discards = e.bank_discards - 1
  return true
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
    local amount = effect.amount * (e.gold_mult or 1)
    p.gold = p.gold + amount
    return "+" .. amount .. " gold"
  elseif effect.type == "score" then
    e.quota = math.max(0, e.quota - effect.amount) -- quota is what is still missing
    e.scored = e.scored + effect.amount -- points earned this level (may overshoot on the last flip)
    return "+" .. effect.amount .. " points"
  elseif effect.type == "energy" then
    p.energy = p.energy + effect.amount
    return "+" .. effect.amount .. " energy"
  elseif effect.type == "all_odds" then
    e.magnet = e.magnet + effect.amount
    return "all coins +" .. math.floor(effect.amount * 100 + .5) .. "% Heads"
  elseif effect.type == "peek" then
    local peek = {}
    for i = 1, 2 do if e.pile[i] then peek[#peek + 1] = e.pile[i] end end
    if #peek > 0 then game.peek_next = peek end -- shown once the next coin is dealt (deal clears the old peek)
    return "peek"
  elseif effect.type == "bank_discard" then
    e.bank_discards = (e.bank_discards or 0) + effect.amount -- spent by Game.discard_bank
    return "discard one"
  elseif effect.type == "extra_exchange" then
    e.extra_exchanges = (e.extra_exchanges or 0) + effect.amount
    return "+" .. effect.amount .. " exchange"
  elseif effect.type == "amplify" then
    -- every active buff lasts one coin longer and gets stronger (x2 -> x3, +20% -> +40% Heads)
    for _, buff in ipairs(e.buffs) do
      buff.left = buff.left + 1
      if buff.kind == "mult" then buff.amount = buff.amount + 1
      elseif buff.kind == "odds" then buff.amount = math.min(.6, buff.amount * 2) end
    end
    return "buffs amplified"
  elseif effect.type == "combo_bonus" then
    e.combo_len = e.combo_len + effect.amount
    return "combo +" .. effect.amount
  elseif effect.type == "combo_shield" then
    e.shield = e.shield + effect.amount
    return "combo shield"
  elseif effect.type == "next_mult" then
    Game.add_buff(game, "mult", effect.amount, effect.coins)
    return "next " .. effect.coins .. " coins x" .. effect.amount
  elseif effect.type == "next_odds" then
    Game.add_buff(game, "odds", effect.amount, effect.coins)
    return "next " .. effect.coins .. " coins +" .. math.floor(effect.amount * 100 + .5) .. "% Heads"
  elseif effect.type == "next_swap" then
    Game.add_buff(game, "swap", 0, effect.coins)
    return "next coin uses its other side"
  elseif effect.type == "next_heads" then
    Game.add_buff(game, "heads", 0, effect.coins)
    return "next coin lands Heads"
  elseif effect.type == "penalty" then
    e.quota = e.quota + effect.amount -- a penalty moves the goalposts
    e.max_quota = e.max_quota + effect.amount
    return "quota +" .. effect.amount
  elseif effect.type == "extra_draw" then
    -- "extra draw": a played coin goes back into the draw pile (the flipping coin itself, or a random
    -- played one when no coin is flipping), so it can be played again. Capped per level.
    local back = 0
    for _ = 1, effect.amount do
      if e.returned >= Game.RETURN_CAP then break end
      local uid = item and item.uid
      if not uid then
        local candidates = {}
        for _, played in ipairs(e.played) do
          if not e.discarded[played] then candidates[#candidates + 1] = played end
        end
        if #candidates == 0 then break end
        uid = candidates[RNG.int(game, 1, #candidates)]
      end
      for index, played in ipairs(e.played) do
        if played == uid then table.remove(e.played, index) break end
      end
      for _, waiting in ipairs(e.pile) do if waiting == uid then uid = nil break end end -- never twice in the pile
      if not uid then break end
      table.insert(e.pile, RNG.int(game, 1, #e.pile + 1), uid)
      e.returned = e.returned + 1
      back = back + 1
    end
    return back > 0 and (back .. " coin back in the pile") or "no coin could return"
  elseif effect.type == "probability" then
    e.bonus[item.uid] = (e.bonus[item.uid] or 0) + effect.amount
    return "+" .. math.floor(effect.amount * 100 + .5) .. "% Heads this encounter"
  end
  error("unknown effect: " .. tostring(effect.type))
end

-- Stock the shop after a cleared level: four coins and one relic, all from the seeded RNG.
local function enter_shop(game)
  game.phase = "SHOP"
  game.reroll_cost = 4
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

local function close_encounter(game)
  Hooks.unbind()
  game.dealt = nil
  Items.clear()
end

-- Open a fresh shop for a sandbox scene, using the same stock and seeded RNG as a cleared level.
function Game.open_sandbox_shop(game)
  assert(game.sandbox and game.phase == "ENCOUNTER", "sandbox shop needs a fresh encounter")
  close_encounter(game)
  enter_shop(game)
end

-- The level is over for good: the player opened the shop (or the boss fell). Only possible once the
-- quota is met. The payout was already given when the quota was met.
function Game.end_level(game)
  local e = game.encounter
  if game.phase ~= "ENCOUNTER" or not e.cleared or game.pending or game.mulligan then return false end
  close_encounter(game)
  Signal.emit("encounter_end", {game = game, won = true})
  if e.boss then game.phase = "VICTORY" else enter_shop(game) end
  return true
end

function Game.resolve(game)
  if game.phase ~= "ENCOUNTER" or not game.pending then return false end
  local e = game.encounter
  local result = game.pending
  local item = find_coin(game, result.uid)
  e.flips = e.flips + 1
  local final = result.result -- already decided (relics, boss inversion) when the coin was flipped
  result.final = final
  if final == "Heads" then e.streak = e.streak + 1
  elseif final == "Tails" then e.streak = 0 end -- Edge holds the current streak
  -- combo: consecutive identical results. A shield (Anchor) lets one different result pass without breaking it.
  if final ~= "Tie" then
    if e.combo_side == final then e.combo_len = e.combo_len + 1
    elseif e.combo_side and e.shield > 0 then e.shield = e.shield - 1
    else e.combo_side, e.combo_len = final, 1 end
  end
  -- hooks get a private copy of the effect list so they can edit it without touching the def
  local res = {result = final, raw = result.raw, effects = {}}
  if final == "Tie" then
    res.effects = Game.tie_effects(item.id)
  else
    local side = buff_active(game, "swap") and (final == "Heads" and "tails" or "heads") or string.lower(final)
    for i, effect in ipairs(catalog[item.id][side]) do
      res.effects[i] = {type = effect.type, amount = effect.amount, coins = effect.coins}
    end
  end
  Signal.emit("coin_resolve", {game = game, inst = item, res = res})
  if final == "Tails" then e.tails = (e.tails or 0) + 1 end -- Tails flipped this level (Martyr reads it)
  result.base_effects = {} -- what this coin did before multipliers (True Echo repeats it)
  for i, effect in ipairs(res.effects) do
    result.base_effects[i] = {type = effect.type, amount = effect.amount, coins = effect.coins}
  end
  local multiplier = 1
  for _, buff in ipairs(e.buffs) do
    if buff.kind == "mult" and not buff.fresh then multiplier = multiplier * buff.amount end
  end
  local combo = math.min(e.combo_cap, 1 + e.combo_step * (math.max(e.combo_len, 1) - 1))
  if res.cash_out then combo = combo * combo end -- Cash Out spends the combo twice (then resets it)
  result.combo = {len = e.combo_len, mult = combo, side = e.combo_side}
  multiplier = multiplier * combo
  if multiplier ~= 1 then -- buffs ("next coins pay double") and the combo multiply points and gold
    for _, effect in ipairs(res.effects) do
      if effect.type == "score" or effect.type == "gold" then effect.amount = math.floor(effect.amount * multiplier + .5) end
    end
  end
  if res.cash_out then e.combo_side, e.combo_len = nil, 0 end
  local messages = {}
  local scored_before, quota_total_before = e.scored, e.max_quota
  for _, effect in ipairs(res.effects) do
    local text = apply_effect(game, item, effect)
    messages[#messages + 1] = text
    Signal.emit("effect_applied", {game = game, inst = item, effect = effect, text = text})
  end
  tick_buffs(game)
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
  if e.cleared and e.boss then
    Game.end_level(game) -- the boss ends the run the moment its quota is met
  else
    deal(game) -- deals the next coin, or handles an empty stack
  end
  return true
end

local function lose_level(game, why)
  game.phase = "GAME_OVER"
  game.exchange_open = false
  game.dealt = nil
  game.lost_why = why
  log(game, "Defeat: " .. why)
  Hooks.unbind()
  Items.clear()
  Signal.emit("encounter_end", {game = game, won = false})
end

-- Gold an exchange costs right now: it rises with every exchange already made this level.
function Game.exchange_cost(game)
  return Game.EXCHANGE_BASE + Game.EXCHANGE_STEP * (game.encounter.exchanges or 0)
end

-- The coins that were played this level and could come back (discarded coins never do).
local function returnable(game)
  local e = game.encounter
  local list = {}
  for _, uid in ipairs(e.played) do
    if not e.discarded[uid] then list[#list + 1] = uid end
  end
  return list
end

-- Exchanges still allowed this level (EXCHANGE_MAX, plus extras from Lifeline coins and the Bonus Exchange modifier).
function Game.exchanges_left(game)
  local e = game.encounter
  return Game.rule(game, "exchange_max", Game.EXCHANGE_MAX) + (e.extra_exchanges or 0) - (e.exchanges or 0)
end

-- With an empty stack, pay gold to get up to EXCHANGE_GAIN of the coins you already played this level
-- back into the stack.
function Game.can_exchange(game)
  if game.phase ~= "ENCOUNTER" or game.pending or game.mulligan or game.dealt then return false end
  if Game.coins_left(game) > 0 then return false end
  if Game.exchanges_left(game) <= 0 then return false end
  return game.player.gold >= Game.exchange_cost(game) and #returnable(game) >= 1
end

function Game.exchange(game)
  if not Game.can_exchange(game) then return false end
  local e = game.encounter
  local cost = Game.exchange_cost(game)
  game.player.gold = game.player.gold - cost
  local back = returnable(game)
  for i = #back, 2, -1 do -- seeded shuffle, then take the first EXCHANGE_GAIN
    local j = RNG.int(game, 1, i)
    back[i], back[j] = back[j], back[i]
  end
  local taken = {}
  for i = 1, math.min(Game.EXCHANGE_GAIN, #back) do
    taken[back[i]] = true
    e.pile[#e.pile + 1] = back[i]
  end
  local kept = {}
  for _, uid in ipairs(e.played) do
    if not taken[uid] then kept[#kept + 1] = uid end
  end
  e.played = kept
  e.exchanges = (e.exchanges or 0) + 1
  game.exchange_open = false
  log(game, "Paid " .. cost .. " gold: " .. #e.pile .. " coins are back in the stack.")
  deal(game)
  return true
end

-- The stack ran dry. Cleared level: the shop is the only way on (unless an exchange is possible, then
-- the player chooses). Uncleared: exchange if possible (the player decides), otherwise the run ends.
function Game.stack_empty(game)
  local e = game.encounter
  local can_exchange = Game.can_exchange(game)
  if e.cleared then
    if not can_exchange then Game.end_level(game) end
  elseif can_exchange then
    game.exchange_open = true
  else
    lose_level(game, Game.exchanges_left(game) <= 0 and "out of coins, and all exchanges are used."
      or "out of coins, and not enough gold to exchange.")
  end
end

-- Give up instead of exchanging (only while the stack is empty and the quota is unmet).
function Game.give_up(game)
  if game.phase ~= "ENCOUNTER" or game.encounter.cleared or game.dealt or game.pending or game.mulligan
    or Game.coins_left(game) > 0 then return false end
  lose_level(game, "gave up.")
  return true
end

-- Give the player a relic and (re)bind every owned relic to the event bus.
function Game.add_relic(game, id)
  game.relics[#game.relics + 1] = id
  Relics.bind(game)
end

function Game.buy_item(game, index)
  local id = game.phase == "SHOP" and game.shop_items[index]
  if not id or #game.items >= Items.MAX or game.player.gold < Game.price(game, item_catalog[id].cost) then return false end
  game.shop_items[index] = false
  game.player.gold = game.player.gold - Game.price(game, item_catalog[id].cost)
  game.items[#game.items + 1] = id
  log(game, "Bought " .. item_catalog[id].name .. " for " .. Game.price(game, item_catalog[id].cost) .. " gold.")
  return true
end

function Game.use_item(game, slot) return Items.use(game, slot) end

function Game.buy_relic(game)
  if game.phase ~= "SHOP" or not game.shop_relic or game.player.gold < Game.price(game, 25) then return false end
  game.player.gold = game.player.gold - Game.price(game, 25)
  Game.add_relic(game, game.shop_relic)
  log(game, "Bought relic " .. relic_catalog[game.shop_relic].name .. " for " .. Game.price(game, 25) .. " gold.")
  game.shop_relic = nil
  return true
end

-- Gold the next deck slot costs: SLOT_COST for the first one, SLOT_STEP more for each one bought before it.
function Game.slot_cost(game)
  return Game.SLOT_COST + Game.SLOT_STEP * (game.slots - Game.START_MAX)
end

-- One more deck slot (the deck starts with START_MAX and can grow to DECK_MAX).
function Game.buy_slot(game)
  local cost = Game.slot_cost(game)
  if game.phase ~= "SHOP" or game.slots >= Game.DECK_MAX or game.player.gold < cost then return false end
  game.player.gold = game.player.gold - cost
  game.slots = game.slots + 1
  log(game, "Bought deck slot " .. game.slots .. " for " .. cost .. " gold.")
  return true
end

function Game.buy(game, index)
  local id = game.phase == "SHOP" and game.shop_offers[index]
  if not id or game.player.gold < Game.price(game, catalog[id].cost or 15) then return false end
  if #game.coins >= game.slots then return false end -- a full deck needs a free slot (buy one) or must lose a coin
  game.shop_offers[index] = false
  game.purchased[id] = true -- the UI turns purchases of locked coins into permanent unlocks
  game.player.gold = game.player.gold - Game.price(game, catalog[id].cost or 15)
  add_to_deck(game, id)
  log(game, "Bought " .. catalog[id].name .. " for " .. Game.price(game, catalog[id].cost or 15) .. " gold.")
  return true
end

-- Rerolling the coin offers costs 4 gold, 2 more each time within one shop visit.
function Game.reroll_shop(game)
  local cost = game.reroll_cost or 4
  if game.phase ~= "SHOP" or game.player.gold < cost then return false end
  game.player.gold = game.player.gold - cost
  game.reroll_cost = cost + 2
  game.shop_offers = shop_stock(game)
  log(game, "Shop rerolled for " .. cost .. " gold.")
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

function Game.leave_shop(game)
  if game.phase ~= "SHOP" then return false end
  return Game.next_encounter(game)
end

-- After the boss: keep going through endless levels (the run is over only when you lose one).
function Game.continue_endless(game)
  if game.phase ~= "VICTORY" or game.endless then return false end
  game.endless = true
  enter_shop(game)
  return true
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
-- A run as plain data (for saving): the whole game table, with the long log trimmed.
function Game.snapshot(game)
  local copy = {}
  for k, v in pairs(game) do copy[k] = v end
  local log_tail = {}
  for i = math.max(1, #game.log - 40), #game.log do log_tail[#log_tail + 1] = game.log[i] end
  copy.log = log_tail
  copy.paused, copy.tutorial = nil, nil
  return copy
end

-- Structural check of a saved run (a damaged or hand-edited file must not crash the game later).
local function valid_run(data)
  local function num(v, low, high) return type(v) == "number" and v == v and v >= (low or -math.huge) and v <= (high or math.huge) and v > -math.huge and v < math.huge end
  local function whole(v, low, high) return num(v, low, high) and v == math.floor(v) end
  local function list(t, check) -- a plain 1..n list whose entries all pass check
    if type(t) ~= "table" then return false end
    local n = 0
    for _ in pairs(t) do n = n + 1 end
    if n ~= #t then return false end
    for i = 1, n do if not check(t[i]) then return false end end
    return true
  end
  local uids = {}
  for _, coin in ipairs(data.coins) do
    if type(coin) ~= "table" or not catalog[coin.id] or not whole(coin.uid) or uids[coin.uid] or not num(coin.bonus) then return false end
    uids[coin.uid] = true
  end
  if not list(data.coins, function(c) return type(c) == "table" end) then return false end
  local function uid_list(t) return list(t, function(u) return uids[u] end) end
  local function uid_map(t, check) -- keyed by existing uids
    if type(t) ~= "table" then return false end
    for uid, v in pairs(t) do if not uids[uid] or not check(v) then return false end end
    return true
  end
  local p = data.player
  if not (num(p.gold, 0) and num(p.energy) and num(p.max_energy)) then return false end
  if not (whole(data.slots, 1, 99) and whole(data.next_uid, 0) and whole(data.encounter_index, 1) and whole(data.cleared, 0)
      and whole(data.rng_state, 1, 2147483646) and whole(data.stake, 1, #stake_catalog)) then return false end
  for uid in pairs(uids) do if uid > data.next_uid then return false end end
  if data.selected_uid ~= nil and not uids[data.selected_uid] then return false end
  if not list(data.log, function(line) return type(line) == "string" end) then return false end
  if not list(data.unlocked, function(id) return catalog[id] ~= nil end) then return false end
  if not (list(data.relics, function(id) return relic_catalog[id] ~= nil end) and list(data.items, function(id) return item_catalog[id] ~= nil end)) then return false end
  if not (list(data.shop_offers, function(id) return id == false or catalog[id] ~= nil end)
      and list(data.shop_items, function(id) return id == false or item_catalog[id] ~= nil end)
      and type(data.purchased) == "table") then return false end
  for id, on in pairs(data.purchased) do if not catalog[id] or on ~= true then return false end end
  if data.shop_relic ~= nil and not relic_catalog[data.shop_relic] then return false end
  local e = data.encounter
  -- in the shop the finished level's table is only kept for display and replaced when the next level starts; a removed coin
  -- leaves its uid in it, so it is not checked further
  if data.phase == "SHOP" then return e == nil or type(e) == "table" end
  if type(e) ~= "table" or type(e.name) ~= "string" or (e.modifier ~= nil and not modifier_catalog[e.modifier]) then return false end
  for _, key in ipairs({"quota", "max_quota", "flips", "scored", "surplus_paid", "discards", "magnet", "streak", "combo_len", "shield",
      "combo_step", "combo_cap", "returned"}) do
    if not num(e[key]) then return false end
  end
  if e.payout ~= nil and not num(e.payout) then return false end -- the House pays nothing
  if e.bank_discards ~= nil and not num(e.bank_discards) then return false end
  if not (uid_list(e.queue) and uid_list(e.pile) and uid_list(e.played) and uid_map(e.discarded, function(v) return v == true end)
      and uid_map(e.bonus, num)) then return false end
  if not list(e.buffs, function(b) return type(b) == "table" and type(b.kind) == "string" and num(b.amount) and num(b.left) end) then return false end
  if data.mulligan ~= nil and not (type(data.mulligan) == "table" and uid_list(data.mulligan.hand)) then return false end
  return true
end

-- Rebuild a game from a snapshot. Only safe points are saved (the opening hand of a level, the shop), where no coin is
-- mid-flip, so only the owned relics need binding again. Returns nil for data that is not a run.
function Game.restore(data)
  if type(data) ~= "table" or type(data.coins) ~= "table" or type(data.player) ~= "table" or not characters[data.character_id] then return nil end
  if data.phase ~= "SHOP" and not (data.phase == "ENCOUNTER" and data.mulligan and data.encounter) then return nil end
  -- a run saved by another build may name content that no longer exists
  if type(data.relics) ~= "table" or type(data.items) ~= "table" then return nil end
  for _, id in ipairs(data.relics) do if not relic_catalog[id] then return nil end end
  for _, id in ipairs(data.items) do if not item_catalog[id] then return nil end end
  if not valid_run(data) then return nil end
  Hooks.unbind()
  Items.clear()
  data.paused = false
  Relics.bind(data)
  return data
end

function Game.relics() return relic_catalog end
function Game.modifiers() return modifier_catalog end
function Game.stakes() return stake_catalog end
function Game.item_catalog() return item_catalog end
function Game.characters() return characters end
function Game.active_count(game) return active_count(game) end
function Game.get_coin(game, uid) return find_coin(game, uid) end

return Game
