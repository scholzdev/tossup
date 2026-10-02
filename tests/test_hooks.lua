local Game = require("src.game")
local Signal = require("src.signal")
local catalog = Game.catalog()

local function equal(a, b, message)
  assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b))
end

-- make the dealt coin the given def and flip it to a forced side
local function play(game, side)
  assert(Game.flip(game))
  if side then game.pending.result = side end
  assert(Game.resolve(game))
end

local characters = Game.characters()

-- new run whose starter (and so first dealt coin) is the given coin id, with a huge enemy
local function fresh(id, seed)
  characters.test = {name = "Test", description = "", starter = id, pool = {"sword", "dagger", "hammer", "blood", "cursed", "focus"}}
  local game = Game.new(seed or 1, "test")
  game.encounter.quota = 1000
  game.encounter.combo_step = 0 -- the combo has its own tests (test_combo.lua)
  game.reshuffle = true -- keep playing the same coins
  return game
end

-- snowball: grow("flip") + on_resolve edit the points, persisting on the instance
local s = fresh("snowball")
local hp = s.encounter.quota
play(s, "Heads")
equal(hp - s.encounter.quota, 3, "snowball first flip")
equal(s.coins[1].stack, 1, "snowball grew")
hp = s.encounter.quota
play(s, "Heads")
equal(hp - s.encounter.quota, 4, "snowball second flip")
hp = s.encounter.quota
play(s, "Tails")
equal(hp - s.encounter.quota, 1, "snowball growth only pays on Heads")

-- momentum: on_odds reads the streak and is pure
local m = fresh("momentum")
m.encounter.streak = 2
assert(math.abs(Game.probability(m, m.coins[1]) - .45) < 1e-9, "momentum odds")
m.encounter.streak = 0
assert(math.abs(Game.probability(m, m.coins[1]) - .35) < 1e-9, "momentum odds reset")

-- gambler: heads is either 0 or 21 points, both occur
local seen = {}
for seed = 1, 60 do
  local g = fresh("gambler", seed)
  local before = g.encounter.quota
  play(g, "Heads")
  seen[before - g.encounter.quota] = true
end
assert(seen[0] and seen[21], "gambler wins and loses")
for k in pairs(seen) do assert(k == 0 or k == 21, "gambler amount " .. k) end

-- on_flip can rewrite the outcome; raw stays as rolled
catalog.t_flip = {name = "T", description = "", probability = 0, heads = {}, tails = {},
  on_flip = function(_, _, flip) flip.result = "Heads" end}
local f = fresh("t_flip")
assert(Game.flip(f))
equal(f.pending.raw, "Tails", "rolled raw")
equal(f.pending.result, "Heads", "hook changed result")

-- register(ctx): scoped to its own coin and torn down after resolve
local calls = {}
catalog.t_reg = {name = "R", description = "", probability = 1, heads = {}, tails = {},
  register = function(ctx)
    ctx.on("coin_deal", function() calls[#calls + 1] = "deal" end)
    ctx.on("coin_resolve", function(e) calls[#calls + 1] = "resolve" e.res.effects[1] = {type = "gold", amount = 7} end)
  end}
calls = {}
local r = fresh("t_reg")
equal(table.concat(calls, ","), "deal", "dealt once")
local gold = r.player.gold
play(r, "Heads")
equal(table.concat(calls, ","), "deal,resolve,deal", "handler ran once, rebound on the redeal")
equal(r.player.gold, gold + 7, "hook-added effect applied")
r.coins[2] = {uid = 60, id = "sword", bonus = 0}
r.encounter.pile = {60}
calls = {}
equal(Game.discard(r), 1) -- t_reg discarded, sword dealt: t_reg handlers must be gone
equal(r.dealt.uid, 60)
play(r, "Heads")
equal(#calls, 0, "handlers unbound once another coin is dealt")

-- discard hook + grow("discard")
local log = {}
catalog.t_disc = {name = "D", description = "", probability = .5, heads = {}, tails = {},
  on_discard = function() log[#log + 1] = "discard" end,
  grow = function(_, ev) log[#log + 1] = "grow:" .. ev end}
local d = fresh("t_disc")
d.coins[2] = {uid = 70, id = "sword", bonus = 0}
log = {}
equal(Game.discard(d), 1)
equal(table.concat(log, ","), "discard,grow:discard", "discard hooks")

-- global events: encounter_start / encounter_end / effect_applied
local events = {}
local handles = {
  Signal.on("encounter_start", function() events[#events + 1] = "start" end),
  Signal.on("encounter_end", function(e) events[#events + 1] = e.won and "won" or "lost" end),
  Signal.on("effect_applied", function(e) events[#events + 1] = e.effect.type end),
}
local ev = fresh("sword", 5)
ev.encounter.quota = 1
events = {}
play(ev, "Heads")
equal(table.concat(events, ","), "score", "quota met, level still open")
assert(Game.end_level(ev))
equal(table.concat(events, ","), "score,won", "encounter_end fires when the level ends")
for _, h in ipairs(handles) do Signal.off(h) end

-- determinism with hooks: same seed, same log
local function run(seed)
  local g = Game.new(seed, "seer")
  g.coins[1].id = "gambler"
  local n = 0
  while g.phase == "ENCOUNTER" and n < 30 do
    n = n + 1
    g.player.energy = 99 -- Gambler costs energy; this test is about determinism
    if g.dealt then play(g) end
  end
  return table.concat(g.log, "\n")
end
equal(run(9), run(9), "hooked run deterministic")

print("hook tests passed")
