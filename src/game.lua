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
local CONTRACTS, CONTRACT_ORDER = {}, {}
for _, def in ipairs(require("content.contracts")) do
  CONTRACTS[def.id] = def
  CONTRACT_ORDER[#CONTRACT_ORDER + 1] = def.id
end
local ENCOUNTERS, ENCOUNTER_ORDER = {}, {}
for _, def in ipairs(require("content.encounters")) do
  ENCOUNTERS[def.id] = def
  ENCOUNTER_ORDER[#ENCOUNTER_ORDER + 1] = def.id
end
local AUGMENTS, AUGMENT_ORDER = {}, {}
for _, def in ipairs(require("content.augments")) do
  AUGMENTS[def.id] = def
  AUGMENT_ORDER[#AUGMENT_ORDER + 1] = def.id
end
local characters = require("content.characters")
local Profile = require("src.profile")
local Game = {}
local TYPE_BUFF_TARGETS = {steel = true, blood = true, greed = true, chaos = true, rhythm = true}

Game.DEV_MODE = os.getenv("TOSSUP_DEV") == "1"
Game.SANDBOX_MODE = os.getenv("SANDBOX") == "1"
Game.VISIBLE = 3 -- coins shown in the bank; the first one is the coin you are about to play
Game.MULLIGAN = 5 -- coins drawn at the start of a level, from which you may discard
Game.START_MAX = 5 -- coins in a coin set = the deck slots a run starts with
Game.DECK_MAX = 10 -- the most deck slots; the shop sells the extra ones one by one
Game.SLOT_COST, Game.SLOT_STEP = 5, 2 -- gold for the first extra deck slot, and how much more every further one costs
Game.EXCHANGE_BASE = 7 -- gold for the first exchange of a level (empty stack): played coins come back
Game.EXCHANGE_STEP = 5 -- every further exchange in the same level costs this much more
Game.EXCHANGE_GAIN = 3 -- played coins that come back into the stack in exchange
Game.COMBO_STEP, Game.COMBO_CAP = 0.25, 3 -- combo: x1 + 0.25 per extra same result in a row, up to x3
Game.EXCHANGE_MAX = 3 -- exchanges per level; after the third one an empty stack loses the level
Game.RETURN_CAP = 3 -- "extra draw" effects (a coin returning to the pile) per level
Game.START_GOLD = 5
Game.SURPLUS_RATE = .5 -- gold per point scored beyond the quota (rounded down in total)

local function combo_pot(len)
  -- Each repeated result adds one more gold to the unbanked triangular pot.
  return math.max(0, (len - 1) * len / 2)
end

local levels = {
  (require("content.levels.opening")),
  (require("content.levels.second_chance")),
  (require("content.levels.high_stakes")),
  (require("content.levels.rising_tide")),
  (require("content.levels.double_down")),
  (require("content.levels.last_call")),
  (require("content.levels.final_table")),
  (require("content.levels.the_house")),
}

local function log(game, message)
  game.log[#game.log + 1] = message
end

local function contract_def(encounter)
  return encounter and encounter.contract and CONTRACTS[encounter.contract.id]
end

local function has_augment(game, id)
  for _, owned in ipairs(game.augments or {}) do
    if owned == id then return true end
  end
  return false
end

local function new_shop_state()
  return {refresh_cost = 4, coin_offer_count = 4, coin_price_discount = 0}
end

-- A selected Run Encounter gets one on_trigger hook; event names identify the lifecycle boundary,
-- and mutable in-flight values are exposed temporarily under game.run (throw, bank, side_bet).
local function trigger_run_encounter(game, event)
  if not game.run_encounter_id then return false end
  local def = ENCOUNTERS[game.run_encounter_id]
  if not def or not def.on_trigger then return false end
  game.run = game.run or {}
  def.on_trigger({game = game, event = event, encounter = game.encounter, shop = game.shop})
  return true
end

local function coin(game, id, upgrade)
  assert(catalog[id], "unknown coin: " .. tostring(id))
  assert(not upgrade or catalog[id].upgrades and catalog[id].upgrades[upgrade], "unknown upgrade for " .. tostring(id))
  game.next_uid = game.next_uid + 1
  return {uid = game.next_uid, id = id, bonus = 0, upgrade = upgrade}
end

local function find_coin(game, uid)
  for _, item in ipairs(game.coins) do
    if item.uid == uid then return item end
  end
end

local function has_type(item, wanted)
  for _, kind in ipairs(catalog[item.id].coin_types or {}) do
    if kind == wanted then return true end
  end
  return false
end

local function active_count(game)
  return #game.coins
end

local function add_to_deck(game, id, upgrade)
  local item = coin(game, id, upgrade)
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

-- Up to four distinct coin offers; one can occasionally carry an upgrade.
local function shop_stock(game)
  local pool = shop_pool(game)
  local ids = offers(game, pool, game.shop and game.shop.coin_offer_count or 4)
  local upgrades, available = {}, {}
  for i = 1, #ids do upgrades[i] = false end
  for _, id in ipairs(pool) do
    local coin_upgrades = catalog[id].upgrades or {}
    local keys = {}
    for key in pairs(coin_upgrades) do keys[#keys + 1] = key end
    table.sort(keys)
    for _, key in ipairs(keys) do available[#available + 1] = {id = id, upgrade = key} end
  end
  if #available > 0 and #ids > 0 and RNG.int(game, 1, 3) == 1 then
    local variant = available[RNG.int(game, 1, #available)]
    local index
    for i, id in ipairs(ids) do if id == variant.id then index = i break end end
    index = index or RNG.int(game, 1, #ids)
    ids[index], upgrades[index] = variant.id, variant.upgrade
  end
  return ids, upgrades
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
  local fortune = has_type(item, "fortune") and (game.fortune_bonus or 0) or 0
  local upgrade = item.upgrade and catalog[item.id].upgrades and catalog[item.id].upgrades[item.upgrade]
  local p = (override and override.heads or catalog[item.id].probability) + (upgrade and upgrade.heads_probability or 0)
    + item.bonus + bonus + magnet + boost + fortune
  local max_heads = 1 - Game.tie_probability(game, item)
  if game.phase ~= "ENCOUNTER" then return math.max(0, math.min(max_heads, p)) end -- odds hooks read the level: none in the shop
  p = Hooks.odds(game, item, p)
  local contract = contract_def(game.encounter)
  if contract then p = p - (contract.heads_penalty or 0) end
  return math.max(0, math.min(max_heads, p))
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
      assert((effect.type == "score" or effect.type == "gold" or effect.type == "gold_loss" or effect.type == "energy" or effect.type == "penalty")
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

-- A level's base quota scales with deck size because a bigger deck means more flips.
-- A level of the run. Beyond the route (after the boss) the levels are endless: each asks 0.5 more points per
-- coin than the one before, pays more, and inverts every 5th flip like The House.
function Game.stage(level)
  if levels[level] then return levels[level] end

  local k = level - #levels

  return {
    name = "Endless",
    endless = k,
    per_coin = levels[#levels].per_coin + 0.5 * k,
    payout = 40 + 5 * k,
    inverts = true,
  }
end

function Game.quota_for(level, coin_count, mult)
  return math.max(1, math.floor(Game.stage(level).per_coin * coin_count * (mult or 1) + .5))
end

local function printed_net(effects)
  local points = 0
  for _, effect in ipairs(effects) do
    if effect.type == "score" then points = points + effect.amount
    elseif effect.type == "penalty" then points = points - effect.amount end
  end
  return points
end

-- Strong decks ask for more than the size-only quota. Use printed expected points plus each coin's
-- estimate for scoring hooks; no RNG is consumed, so inspecting a deck cannot change its flips.
local function deck_quota(game)
  local counts, distinct = {}, 0
  for _, item in ipairs(game.coins) do
    if not counts[item.id] then distinct = distinct + 1 end
    counts[item.id] = (counts[item.id] or 0) + 1
  end
  local power = 0
  for _, item in ipairs(game.coins) do
    local def = catalog[item.id]
    local upgrade = item.upgrade and def.upgrades and def.upgrades[item.upgrade]
    local odds = game.sandbox and game.sandbox.odds and game.sandbox.odds[item.id]
    local tie = odds and odds.tie or def.tie_probability or 0
    local heads = (odds and odds.heads or def.probability) + (upgrade and upgrade.heads_probability or 0) + item.bonus
    if has_type(item, "fortune") then heads = heads + (game.fortune_bonus or 0) end
    heads = math.max(0, math.min(1 - tie, heads))
    local tails = 1 - heads - tie
    local expected = heads * (printed_net(def.heads) + (upgrade and upgrade.heads_score or 0)) + tails * printed_net(def.tails)
    if tie > 0 then expected = expected + tie * printed_net(Game.tie_effects(item.id)) end
    if def.quota_extra then expected = expected + def.quota_extra(game, item, heads, tails, counts, distinct) end
    power = power + math.max(0, expected)
  end
  local mult = Game.rule(game, "quota_mult", 1)
  local base = Game.quota_for(game.encounter_index, #game.coins, mult)
  local excess = math.max(0, power - 1.5 * #game.coins)
  return base + math.floor(excess * 1.3 * mult + .5)
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

function Game.coin_offer_cost(game, index)
  local id = game.phase == "SHOP" and game.shop_offers[index]
  if not id then return nil end
  local upgrade_id = game.shop_upgrades and game.shop_upgrades[index]
  local upgrade = upgrade_id and catalog[id].upgrades and catalog[id].upgrades[upgrade_id]
  local cost = Game.price(game, (catalog[id].cost or 15) + (upgrade and upgrade.cost or 0))
  cost = math.max(0, cost - (game.shop and game.shop.coin_price_discount or 0))
  return cost
end

local function start_encounter(game)
  local stage = Game.stage(game.encounter_index)
  local quota = deck_quota(game) + (game.next_level_quota_bonus or 0)
  game.next_level_quota_bonus = 0
  game.encounter = nil -- discards from the previous level must not carry over
  game.encounter = {name = stage.name, quota = quota, max_quota = quota,
    boss = stage.boss or false, inverts = stage.boss or stage.inverts or false, endless = stage.endless, payout = stage.payout,
    flips = 0, scored = 0, cleared = false, surplus_paid = 0, discarded = {}, discards = 0, bonus = {}, magnet = 0, streak = 0, buffs = {}, combo_side = nil, combo_len = 0, combo_pot = 0, shield = 0, combo_step = Game.COMBO_STEP, combo_cap = Game.COMBO_CAP,
    returned = 0, played = {}, best_scores = {}, best_combo_len = 0, pile = shuffle_deck(game), queue = {}}
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
  trigger_run_encounter(game, "encounter_start")
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

function Game.offer_contract(game)
  local e = game.encounter
  if game.phase ~= "ENCOUNTER" or not e or e.flips > 0 then return false end
  local pool = {}
  for _, id in ipairs(CONTRACT_ORDER) do
    if id ~= "amazon_prime" or not e.boss then pool[#pool + 1] = id end
  end
  e.contract_options = offers(game, pool, 3)
  game.phase = "CONTRACT"
  return true
end

function Game.choose_contract(game, id)
  if game.phase ~= "CONTRACT" then return false end
  local e = game.encounter
  for _, offered_id in ipairs(e.contract_options or {}) do
    if offered_id == id and CONTRACTS[id] then
      local def = CONTRACTS[id]
      e.contract = {id = id}
      e.contract_options = nil
      game.phase = "ENCOUNTER"
      log(game, "Accepted contract: " .. def.name .. " (" .. (def.reward_text or (def.reward .. " gold")) .. ").")
      return true
    end
  end
  return false
end

function Game.contract_def(id) return CONTRACTS[id] end
function Game.encounter_def(id) return ENCOUNTERS[id] end
function Game.augment_def(id) return AUGMENTS[id] end
function Game.encounters() return ENCOUNTER_ORDER end
function Game.augments() return AUGMENT_ORDER end

function Game.skip_contract(game)
  if game.phase ~= "CONTRACT" or not game.encounter then return false end
  game.encounter.contract_options = nil
  game.phase = "ENCOUNTER"
  log(game, "Contract skipped.")
  return true
end

function Game.offer_augment(game, level)
  local target = level or (game.encounter_index + 1)
  if game.phase ~= "SHOP" or game.sandbox or target ~= game.encounter_index + 1
      or (target ~= 3 and target ~= 6) then return false end
  local owned = {}
  for _, id in ipairs(game.augments or {}) do owned[id] = true end
  local pool = {}
  for _, id in ipairs(AUGMENT_ORDER) do
    if not owned[id] then pool[#pool + 1] = id end
  end
  if #pool < 3 then return false end
  game.augment_level = target
  game.augment_options = offers(game, pool, 3)
  game.phase = "AUGMENT"
  return true
end

function Game.choose_augment(game, id)
  if game.phase ~= "AUGMENT" then return false end
  local offered = false
  for _, offered_id in ipairs(game.augment_options or {}) do
    if offered_id == id and AUGMENTS[id] then offered = true break end
  end
  if not offered then return false end
  game.augments = game.augments or {}
  game.augments[#game.augments + 1] = id
  local level = game.augment_level
  game.augment_level, game.augment_options = nil, nil
  game.encounter_index = level
  log(game, "Chosen augment: " .. AUGMENTS[id].name .. ".")
  start_encounter(game)
  if game.contracts_enabled then Game.offer_contract(game) end
  return true
end

local function settle_contract(game)
  local e, contract = game.encounter, game.encounter.contract
  if not contract or contract.result then return end
  local def = CONTRACTS[contract.id]
  if not def then return end
  local complete = def.complete(e)
  contract.result = complete and "COMPLETE" or "MISSED"
  if complete then
    local reward = def.reward or 0
    game.player.gold = game.player.gold + reward
    if reward > 0 then
      log(game, "Contract complete: +" .. reward .. " gold.")
    else
      log(game, "Contract complete: " .. def.name .. ".")
    end
  else
    log(game, "Contract missed: " .. def.name .. ".")
  end
end

local function apply_contract_callback(game, encounter)
  local contract = encounter.contract
  if not contract or not contract.result then return end
  local def = CONTRACTS[contract.id]
  if not def then return end
  local callback
  if contract.result == "COMPLETE" then callback = def.on_success else callback = def.on_failure end
  if callback then callback({game = game}, encounter) end
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
    pending = nil, shop_offers = {}, shop_upgrades = {}, log = {}, selected_uid = nil, slots = Game.START_MAX, sandbox = sandbox,
    fortune_bonus = 0, augments = {}, next_level_quota_bonus = 0,
    run = {},
    shop = new_shop_state()}
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
  if not sandbox then
    game.run_encounter_id = ENCOUNTER_ORDER[RNG.int(game, 1, #ENCOUNTER_ORDER)]
    trigger_run_encounter(game, "run_start")
  end
  start_encounter(game)
  if game.run_encounter_id then
    log(game, "Run Encounter: " .. ENCOUNTERS[game.run_encounter_id].name .. ".")
  end
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
  flip.count = nth
  local before = flip.result
  local outcome = {game = game, inst = item, flips = nth, result = before}
  Signal.emit("coin_outcome", outcome)
  local final = outcome.result
  flip.altered = final ~= before and "RELIC" or nil -- shown in the UI so a changed side is never a mystery
  local interval = Game.rule(game, "boss_every", 5)
  local inverted = e.boss or e.inverts
  flip.stage_inverted = false
  if not outcome.final and final ~= "Tie" and inverted and nth % interval == 0 then
    final = final == "Heads" and "Tails" or "Heads"
    flip.stage_inverted = true
    flip.altered = (flip.altered and flip.altered .. " + " or "") .. "THE HOUSE"
  end
  flip.result = final
  game.run = game.run or {}
  game.run.throw = flip
  trigger_run_encounter(game, "throw")
  game.run.throw = nil
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

local function tick_buffs(game, item)
  local list, kept = game.encounter.buffs, {}
  for _, buff in ipairs(list) do
    if buff.fresh then buff.fresh = false
    elseif not TYPE_BUFF_TARGETS[buff.kind] or has_type(item, buff.kind) then buff.left = buff.left - 1 end
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

function Game.side_bet_quote(game, side)
  if not game.dealt or (side ~= "Heads" and side ~= "Tails") then return nil end
  local odds = side == "Heads" and game.dealt.probability or
    1 - game.dealt.probability - (game.dealt.tie_probability or 0)
  if odds <= 0 then return nil end
  local stake = 5
  local payout = math.max(stake, math.floor(stake / odds + .5))
  if has_augment(game, "hedge_fund") then payout = math.floor(payout * 1.25 + .5) end
  local quote = {side = side, stake = stake, payout = payout}
  game.run = game.run or {}
  game.run.side_bet = quote
  trigger_run_encounter(game, "side_bet_quote")
  game.run.side_bet = nil
  return quote
end

-- One optional odds bet per level, available only before its first flip.
function Game.place_side_bet(game, side)
  local e = game.encounter
  if game.phase ~= "ENCOUNTER" or not e or e.flips ~= 0 or not game.dealt or e.side_bet then return false end
  local quote = Game.side_bet_quote(game, side)
  if not quote or game.player.gold < quote.stake then return false end
  game.player.gold = game.player.gold - quote.stake
  e.side_bet = {side = side, stake = quote.stake, payout = quote.payout}
  log(game, "Bet " .. quote.stake .. " gold on " .. side .. " (" .. quote.payout .. " gold payout).")
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

function Game.can_bank_combo(game)
  local e = game and game.encounter
  return game ~= nil and game.phase == "ENCOUNTER" and e ~= nil and not game.pending and not game.mulligan
    and (e.combo_pot or 0) > 0
end

local function bank_combo_pot(game, counts_for_contract)
  local e, amount = game.encounter, game.encounter.combo_pot or 0
  if amount <= 0 then return 0 end
  if has_augment(game, "bankers_cut") then
    amount = amount + 2
    game.next_level_quota_bonus = (game.next_level_quota_bonus or 0) + 2
  end
  game.run = game.run or {}
  game.run.bank = {amount = amount}
  trigger_run_encounter(game, "bank")
  amount = math.max(0, math.floor(game.run.bank.amount or amount))
  game.run.bank = nil
  game.player.gold = game.player.gold + amount
  if counts_for_contract then e.combo_banked = true end
  e.combo_pot, e.combo_side, e.combo_len = 0, nil, 0
  log(game, "Banked " .. amount .. " combo gold.")
  return amount
end

function Game.bank_combo(game)
  if not Game.can_bank_combo(game) then return 0 end
  return bank_combo_pot(game, true)
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
    if has_augment(game, "scrap_dealer") then
      game.player.gold = game.player.gold + 1
      e.max_quota = e.max_quota + 2
      if not e.cleared then e.quota = e.quota + 2 end
      log(game, "Scrap Dealer: +1 gold, quota +2.")
    end
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

local function return_played_coin(game, uid)
  local e = game.encounter
  if not uid or e.returned >= Game.RETURN_CAP or e.discarded[uid] then return false end
  for _, waiting in ipairs(e.queue) do if waiting == uid then return false end end
  for _, waiting in ipairs(e.pile) do if waiting == uid then return false end end
  for index, played in ipairs(e.played) do
    if played == uid then
      table.remove(e.played, index)
      table.insert(e.pile, RNG.int(game, 1, #e.pile + 1), uid)
      e.returned = e.returned + 1
      return true
    end
  end
  return false
end

local function apply_effect(game, item, effect)
  local p, e = game.player, game.encounter
  if effect.type == "gold" then
    local amount = effect.amount * (e.gold_mult or 1)
    p.gold = p.gold + amount
    return "+" .. amount .. " gold"
  elseif effect.type == "gold_loss" then
    local lost = math.min(p.gold, effect.amount)
    p.gold = p.gold - lost
    return "-" .. lost .. " gold"
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
  elseif effect.type == "fortune_odds" then
    if item.compost_level == game.encounter_index then return "Fortune already fertilized this level" end
    item.compost_level = game.encounter_index
    game.fortune_bonus = math.min(.55, (game.fortune_bonus or 0) + effect.amount)
    return "Fortune +" .. math.floor(game.fortune_bonus * 100 + .5) .. "% Heads for the run"
  elseif effect.type == "type_buff" then
    Game.add_buff(game, effect.kind, 0, effect.coins)
    return "next " .. effect.coins .. " " .. effect.kind .. " coins"
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
      if return_played_coin(game, uid) then back = back + 1 else break end
    end
    return back > 0 and (back .. " coin back in the pile") or "no coin could return"
  elseif effect.type == "fetch_best" then
    if item.fetched_level == game.encounter_index then return "already fetched this level" end
    local best_uid, best_score = nil, 0
    for _, uid in ipairs(e.played) do
      local score = e.best_scores[uid] or 0
      if uid ~= item.uid and score > best_score then best_uid, best_score = uid, score end
    end
    if not return_played_coin(game, best_uid) then return "no scored coin to fetch" end
    item.fetched_level = game.encounter_index
    return catalog[find_coin(game, best_uid).id].name .. " fetched back into the pile"
  elseif effect.type == "probability" then
    e.bonus[item.uid] = (e.bonus[item.uid] or 0) + effect.amount
    return "+" .. math.floor(effect.amount * 100 + .5) .. "% Heads this encounter"
  end
  error("unknown effect: " .. tostring(effect.type))
end

-- Stock the shop after a cleared level using the run's settings and seeded RNG.
local function enter_shop(game)
  game.phase = "SHOP"
  trigger_run_encounter(game, "shop_open")
  game.reroll_cost = game.shop.refresh_cost or 4
  game.reroll_step = 2
  game.shop_offers, game.shop_upgrades = shop_stock(game)
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
  if (e.combo_pot or 0) > 0 then bank_combo_pot(game) end
  close_encounter(game)
  Signal.emit("encounter_end", {game = game, won = true})
  if e.boss then game.phase = "VICTORY" else enter_shop(game) end
  apply_contract_callback(game, e)
  return true
end

function Game.resolve(game)
  if game.phase ~= "ENCOUNTER" or not game.pending then return false end
  local e = game.encounter
  local contract_def_active = contract_def(e)
  local result = game.pending
  local item = find_coin(game, result.uid)
  e.flips = e.flips + 1
  local final = result.result -- already decided (relics, boss inversion) when the coin was flipped
  result.final = final
  game.run = game.run or {}
  game.run.throw = result
  if e.side_bet and not e.side_bet.settled then
    local bet = e.side_bet
    game.run.side_bet = bet
    trigger_run_encounter(game, "side_bet_resolve")
    game.run.side_bet = nil
    if not bet.settled then
      bet.settled = true
      if final == "Tie" then
        game.player.gold = game.player.gold + bet.stake
        bet.outcome = "PUSH"
      elseif final == bet.side then
        game.player.gold = game.player.gold + bet.payout
        bet.outcome = "WON"
      else
        bet.outcome = "LOST"
      end
    end
    if bet.outcome == "PUSH" then log(game, "Side bet pushed; stake returned.")
    elseif bet.outcome == "WON" then log(game, "Side bet won: +" .. bet.payout .. " gold.")
    else
      log(game, "Side bet lost.")
      if has_augment(game, "hedge_fund") then
        game.next_level_quota_bonus = (game.next_level_quota_bonus or 0) + 2
      end
    end
  end
  if final == "Heads" then e.streak = e.streak + 1
  elseif final == "Tails" then e.streak = 0 end -- Edge holds the current streak
  -- combo: consecutive identical results. A shield (Anchor) lets one different result pass without breaking it.
  local previous_combo_side = e.combo_side
  trigger_run_encounter(game, "combo")
  local pushing = e.pushing == true
  e.pushing = nil
  local all_in_multiplier, all_in_message = 1, nil
  if pushing and has_augment(game, "all_in") then
    if final ~= "Tie" and final == previous_combo_side then
      if not e.all_in_paid then
        e.all_in_paid = true
        all_in_multiplier = 2
        all_in_message = "All-In push succeeded: combo payout doubled."
      end
    else
      local lost = math.min(2, game.player.gold)
      game.player.gold = game.player.gold - lost
      all_in_message = "All-In push failed: -" .. lost .. " gold."
    end
  end
  if final == "Tie" and e.combo_side and contract_def_active and contract_def_active.tie_breaks_combo then
    e.combo_side, e.combo_len = nil, 0
  elseif final ~= "Tie" then
    if e.combo_side == final then e.combo_len = e.combo_len + 1
    elseif e.combo_side and e.shield > 0 then e.shield = e.shield - 1
    else e.combo_side, e.combo_len = final, 1 end
  end
  e.best_combo_len = math.max(e.best_combo_len or 0, e.combo_len)
  local combo_broke = previous_combo_side ~= nil and e.combo_side ~= previous_combo_side
  local combo_lost = combo_broke and (e.combo_pot or 0) or 0
  if combo_broke then e.combo_pot = 0 end
  game.run.throw = nil
  -- hooks get a private copy of the effect list so they can edit it without touching the def
  local res = {result = final, raw = result.raw, effects = {}}
  local side
  if final == "Tie" then
    res.effects = Game.tie_effects(item.id)
  else
    side = buff_active(game, "swap") and (final == "Heads" and "tails" or "heads") or string.lower(final)
    for i, effect in ipairs(catalog[item.id][side]) do
      res.effects[i] = {type = effect.type, amount = effect.amount, coins = effect.coins, kind = effect.kind}
    end
  end
  local upgrade = item.upgrade and catalog[item.id].upgrades and catalog[item.id].upgrades[item.upgrade]
  if side == "heads" and upgrade and upgrade.heads_score then
    res.effects[#res.effects + 1] = {type = "score", amount = upgrade.heads_score}
  end
  Signal.emit("coin_resolve", {game = game, inst = item, res = res})
  for _, buff in ipairs(e.buffs) do
    if not buff.fresh and TYPE_BUFF_TARGETS[buff.kind] and has_type(item, buff.kind) then
      if buff.kind == "steel" then
        if final == "Heads" then res.effects[#res.effects + 1] = {type = "score", amount = 3}
        elseif final == "Tails" then res.effects[#res.effects + 1] = {type = "penalty", amount = 2} end
      elseif buff.kind == "blood" then
        if final == "Heads" or final == "Tie" then res.effects[#res.effects + 1] = {type = "score", amount = final == "Tie" and 4 or 8} end
        if final == "Tails" or final == "Tie" then res.effects[#res.effects + 1] = {type = "penalty", amount = final == "Tie" and 2 or 4} end
      elseif buff.kind == "chaos" then
        local count = #res.effects
        for i = 1, count do
          local effect = res.effects[i]
          res.effects[#res.effects + 1] = {type = effect.type, amount = effect.amount, coins = effect.coins,
            kind = effect.kind}
        end
      elseif buff.kind == "rhythm" then
        if combo_broke then res.effects[#res.effects + 1] = {type = "penalty", amount = 5}
        else
          local points = math.min(8, 2 * math.max(0, e.combo_len - 1))
          if points > 0 then res.effects[#res.effects + 1] = {type = "score", amount = points} end
        end
      end
    end
  end
  if final == "Tails" then e.tails = (e.tails or 0) + 1 end -- Tails flipped this level (Martyr reads it)
  result.base_effects = {} -- what this coin did before multipliers (True Echo repeats it)
  for i, effect in ipairs(res.effects) do
    result.base_effects[i] = {type = effect.type, amount = effect.amount, coins = effect.coins, kind = effect.kind}
  end
  local multiplier = 1
  for _, buff in ipairs(e.buffs) do
    if buff.kind == "mult" and not buff.fresh then multiplier = multiplier * buff.amount end
  end
  local combo = math.min(e.combo_cap, 1 + e.combo_step * (math.max(e.combo_len, 1) - 1))
  combo = combo * all_in_multiplier
  if res.cash_out then combo = combo * combo end -- Cash Out spends the combo twice (then resets it)
  if contract_def_active and contract_def_active.combo_multiplier_cap then
    combo = math.min(combo, contract_def_active.combo_multiplier_cap)
  end
  result.combo = {len = e.combo_len, mult = combo, side = e.combo_side}
  multiplier = multiplier * combo
  if multiplier ~= 1 then -- buffs ("next coins pay double") and the combo multiply points and gold
    for _, effect in ipairs(res.effects) do
      if effect.type == "score" or effect.type == "gold" then effect.amount = math.floor(effect.amount * multiplier + .5) end
    end
  end
  local messages = {}
  if all_in_message then messages[#messages + 1] = all_in_message end
  if combo_lost > 0 then messages[#messages + 1] = "lost " .. combo_lost .. " unbanked combo gold" end
  local scored_before, quota_total_before = e.scored, e.max_quota
  local greed = 0
  if has_type(item, "greed") then
    for _, buff in ipairs(e.buffs) do
      if buff.kind == "greed" and not buff.fresh then greed = greed + 1 end
    end
  end
  for _, effect in ipairs(res.effects) do
    local gold_before = game.player.gold
    local text = apply_effect(game, item, effect)
    messages[#messages + 1] = text
    Signal.emit("effect_applied", {game = game, inst = item, effect = effect, text = text})
    local gained_gold = math.max(0, game.player.gold - gold_before)
    if greed > 0 and gained_gold > 0 then
      local extra = gained_gold * (2 ^ greed - 1)
      game.player.gold = game.player.gold + extra
      messages[#messages + 1] = "+" .. extra .. " greed gold"
    end
  end
  if final == "Tails" and contract_def_active and contract_def_active.tails_quota_penalty then
    messages[#messages + 1] = "contract: " .. apply_effect(game, item,
      {type = "penalty", amount = contract_def_active.tails_quota_penalty})
  end
  if greed > 0 then
    if final == "Tails" then
      local lost = math.min(game.player.gold, 3 * greed)
      game.player.gold = game.player.gold - lost
      messages[#messages + 1] = "-" .. lost .. " greed gold"
    end
  end
  e.combo_pot = combo_pot(e.combo_len)
  if res.cash_out then
    local banked = bank_combo_pot(game, true)
    if banked > 0 then messages[#messages + 1] = "banked " .. banked .. " combo gold" end
    e.combo_side, e.combo_len, e.combo_pot = nil, 0, 0
  end
  tick_buffs(game, item)
  result.gained = e.scored - scored_before -- for the UI: what this flip was worth
  result.penalty = e.max_quota - quota_total_before
  e.best_scores[item.uid] = math.max(e.best_scores[item.uid] or 0, result.gained)
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
    settle_contract(game)
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
  local e = game.encounter
  if e and (e.combo_pot or 0) > 0 then
    log(game, "Lost " .. e.combo_pot .. " unbanked combo gold.")
    e.combo_pot = 0
  end
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
  local upgrade_id = id and game.shop_upgrades and game.shop_upgrades[index] or nil
  if upgrade_id and not (catalog[id].upgrades and catalog[id].upgrades[upgrade_id]) then upgrade_id = nil end
  local cost = id and Game.coin_offer_cost(game, index)
  if not id or not cost or game.player.gold < cost then return false end
  if #game.coins >= game.slots then return false end -- a full deck needs a free slot (buy one) or must lose a coin
  game.shop_offers[index] = false
  if game.shop_upgrades then game.shop_upgrades[index] = false end
  game.purchased[id] = true -- the UI turns purchases of locked coins into permanent unlocks
  game.player.gold = game.player.gold - cost
  add_to_deck(game, id, upgrade_id)
  local suffix = upgrade_id and (" (" .. catalog[id].upgrades[upgrade_id].name .. ")") or ""
  log(game, "Bought " .. catalog[id].name .. suffix .. " for " .. cost .. " gold.")
  return true
end

-- Rerolling the coin offers costs 4 gold, 2 more each time within one shop visit.
function Game.reroll_shop(game)
  local cost = game.reroll_cost or 4
  if game.phase ~= "SHOP" or game.player.gold < cost then return false end
  game.player.gold = game.player.gold - cost
  game.reroll_cost = cost + (game.reroll_step or 2)
  game.shop_offers, game.shop_upgrades = shop_stock(game)
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
  local next_level = game.encounter_index + 1
  if not game.endless and Game.offer_augment(game, next_level) then return true end
  game.encounter_index = next_level
  start_encounter(game)
  if game.contracts_enabled then Game.offer_contract(game) end
  return true
end

-- Meta currency earned by a run: 1 per level cleared, +3 for beating the boss.
function Game.run_tokens(game)
  return game.cleared + (game.phase == "VICTORY" and 3 or 0)
end

Game.route = levels -- level definitions; tools/sim.lua overrides them for balance sweeps
Game.apply_effect = apply_effect -- for hooks: apply an effect table to the running game
Game.log = log
function Game.catalog() return catalog end
-- A run as plain data (for saving): the whole game table, with the long log trimmed.
function Game.snapshot(game)
  local copy = {}
  for k, v in pairs(game) do copy[k] = v end
  copy.run = {}
  for k, v in pairs(game.run or {}) do
    if k ~= "throw" and k ~= "bank" and k ~= "side_bet" then copy.run[k] = v end
  end
  copy.run_rules, copy.run_encounter_applied = nil, nil
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
  if data.contracts_enabled ~= nil and type(data.contracts_enabled) ~= "boolean" then return false end
  for _, coin in ipairs(data.coins) do
    if type(coin) ~= "table" or not catalog[coin.id] or not whole(coin.uid) or uids[coin.uid] or not num(coin.bonus) then return false end
    if coin.upgrade ~= nil and (type(coin.upgrade) ~= "string" or not catalog[coin.id].upgrades or not catalog[coin.id].upgrades[coin.upgrade]) then return false end
    if coin.compost_level ~= nil and not whole(coin.compost_level, 1) then return false end
    if coin.fetched_level ~= nil and not whole(coin.fetched_level, 1) then return false end
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
  if data.fortune_bonus ~= nil and not num(data.fortune_bonus, 0, .55) then return false end
  if data.reroll_step ~= nil and not whole(data.reroll_step, 1, 2) then return false end
  if not (whole(data.slots, 1, 99) and whole(data.next_uid, 0) and whole(data.encounter_index, 1) and whole(data.cleared, 0)
      and whole(data.rng_state, 1, 2147483646) and whole(data.stake, 1, #stake_catalog)) then return false end
  if data.run_encounter_id ~= nil and not ENCOUNTERS[data.run_encounter_id] then return false end
  if data.run ~= nil and type(data.run) ~= "table" then return false end
  if data.shop ~= nil then
    if type(data.shop) ~= "table" then return false end
    if data.shop.refresh_cost ~= nil and not num(data.shop.refresh_cost, 0) then return false end
    if data.shop.coin_offer_count ~= nil and not whole(data.shop.coin_offer_count, 1, 99) then return false end
    if data.shop.coin_price_discount ~= nil and not num(data.shop.coin_price_discount, 0) then return false end
  end
  if data.next_level_quota_bonus ~= nil and not whole(data.next_level_quota_bonus, 0) then return false end
  if data.augments ~= nil then
    if not list(data.augments, function(id) return AUGMENTS[id] ~= nil end) or #data.augments > 2 then return false end
    local seen = {}
    for _, id in ipairs(data.augments) do if seen[id] then return false else seen[id] = true end end
  end
  if data.phase == "AUGMENT" then
    if not whole(data.augment_level) or data.augment_level ~= data.encounter_index + 1
        or (data.augment_level ~= 3 and data.augment_level ~= 6)
        or not list(data.augment_options, function(id) return AUGMENTS[id] ~= nil end)
        or #data.augment_options ~= 3 then return false end
    local seen = {}
    for _, id in ipairs(data.augment_options) do
      if seen[id] or has_augment(data, id) then return false end
      seen[id] = true
    end
  elseif data.augment_level ~= nil or data.augment_options ~= nil then
    return false
  end
  for uid in pairs(uids) do if uid > data.next_uid then return false end end
  if data.selected_uid ~= nil and not uids[data.selected_uid] then return false end
  if not list(data.log, function(line) return type(line) == "string" end) then return false end
  if not list(data.unlocked, function(id) return catalog[id] ~= nil end) then return false end
  if not (list(data.relics, function(id) return relic_catalog[id] ~= nil end) and list(data.items, function(id) return item_catalog[id] ~= nil end)) then return false end
  if not (list(data.shop_offers, function(id) return id == false or catalog[id] ~= nil end)
      and list(data.shop_items, function(id) return id == false or item_catalog[id] ~= nil end)
      and type(data.purchased) == "table") then return false end
  if data.shop_upgrades ~= nil then
    if not list(data.shop_upgrades, function(id) return id == false or type(id) == "string" end)
        or #data.shop_upgrades > #data.shop_offers then return false end
    for i, upgrade_id in ipairs(data.shop_upgrades) do
      if upgrade_id ~= false then
        local id = data.shop_offers[i]
        if not id or id == false or not catalog[id].upgrades or not catalog[id].upgrades[upgrade_id] then return false end
      end
    end
  end
  for id, on in pairs(data.purchased) do if not catalog[id] or on ~= true then return false end end
  if data.shop_relic ~= nil and not relic_catalog[data.shop_relic] then return false end
  local e = data.encounter
  -- in the shop the finished level's table is only kept for display and replaced when the next level starts; a removed coin
  -- leaves its uid in it, so it is not checked further
  if data.phase == "SHOP" then return e == nil or type(e) == "table" end
  if type(e) ~= "table" or type(e.name) ~= "string" or (e.modifier ~= nil and not modifier_catalog[e.modifier]) then return false end
  if e.best_combo_len ~= nil and not whole(e.best_combo_len, 0) then return false end
  if e.combo_banked ~= nil and type(e.combo_banked) ~= "boolean" then return false end
  if e.contract ~= nil and (type(e.contract) ~= "table" or not CONTRACTS[e.contract.id]
    or (e.contract.result ~= nil and e.contract.result ~= "COMPLETE" and e.contract.result ~= "MISSED")) then return false end
  if e.contract_options ~= nil then
    if data.phase ~= "CONTRACT" or e.contract ~= nil
      or not list(e.contract_options, function(id) return CONTRACTS[id] ~= nil end) or #e.contract_options ~= 3 then return false end
    local seen = {}
    for _, id in ipairs(e.contract_options) do if seen[id] then return false else seen[id] = true end end
  end
  if data.phase == "CONTRACT" and (data.contracts_enabled ~= true or e.contract_options == nil or data.mulligan == nil) then return false end
  for _, key in ipairs({"quota", "max_quota", "flips", "scored", "surplus_paid", "discards", "magnet", "streak", "combo_len", "shield",
      "combo_step", "combo_cap", "returned"}) do
    if not num(e[key]) then return false end
  end
  if e.combo_pot ~= nil and not whole(e.combo_pot, 0) then return false end
  if e.payout ~= nil and not num(e.payout) then return false end -- the House pays nothing
  if e.bank_discards ~= nil and not num(e.bank_discards) then return false end
  if not (uid_list(e.queue) and uid_list(e.pile) and uid_list(e.played) and uid_map(e.discarded, function(v) return v == true end)
      and uid_map(e.bonus, num)) then return false end
  if e.best_scores ~= nil and not uid_map(e.best_scores, function(v) return num(v, 0) end) then return false end
  if not list(e.buffs, function(b) return type(b) == "table" and type(b.kind) == "string" and num(b.amount) and num(b.left) end) then return false end
  if data.mulligan ~= nil and not (type(data.mulligan) == "table" and uid_list(data.mulligan.hand)) then return false end
  return true
end

-- Rebuild a game from a snapshot. Only safe points are saved (the opening hand of a level, the shop), where no coin is
-- mid-flip, so only the owned relics need binding again. Returns nil for data that is not a run.
function Game.restore(data)
  if type(data) ~= "table" or type(data.coins) ~= "table" or type(data.player) ~= "table" or not characters[data.character_id] then return nil end
  if data.phase ~= "SHOP" and not (data.phase == "AUGMENT" and data.encounter)
      and not ((data.phase == "ENCOUNTER" or data.phase == "CONTRACT") and data.mulligan and data.encounter) then return nil end
  -- a run saved by another build may name content that no longer exists
  if type(data.relics) ~= "table" or type(data.items) ~= "table" then return nil end
  for _, id in ipairs(data.relics) do if not relic_catalog[id] then return nil end end
  for _, id in ipairs(data.items) do if not item_catalog[id] then return nil end end
  if not valid_run(data) then return nil end
  Hooks.unbind()
  Items.clear()
  data.fortune_bonus = data.fortune_bonus or 0
  data.augments = data.augments or {}
  data.next_level_quota_bonus = data.next_level_quota_bonus or 0
  data.shop = data.shop or new_shop_state()
  data.run = data.run or {}
  data.run.throw, data.run.bank, data.run.side_bet = nil, nil, nil
  data.run_rules, data.run_encounter_applied = nil, nil
  if data.phase == "ENCOUNTER" or data.phase == "CONTRACT" then
    data.encounter.best_scores = data.encounter.best_scores or {}
    data.encounter.combo_pot = data.encounter.combo_pot or 0
    data.encounter.best_combo_len = data.encounter.best_combo_len or 0
  end
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
function Game.is_dev() return Game.DEV_MODE end
function Game.is_sandbox() return Game.SANDBOX_MODE end

return Game
