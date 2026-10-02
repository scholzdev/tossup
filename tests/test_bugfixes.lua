-- Regression tests for bugs found in review: Double Down, Echo, Fuse, Lucky Charm, Martyr, profile saving, stages.
package.path = "./?.lua;" .. package.path
local Game = require("src.game")
local Profile = require("src.profile")

local function equal(a, b, message) assert(a == b, (message or "values differ") .. ": " .. tostring(a) .. " ~= " .. tostring(b)) end

Game.use_modifiers = false
local function level(ids, seed)
  Game.characters().test = {name = "Test", description = "", starter = "normal", pool = {}}
  local plain = {}
  for i = 1, #ids do plain[i] = "normal" end
  local keep = Game.START_MAX
  Game.START_MAX = 10
  Game.characters().test.deck = plain -- constructed test stacks may exceed deckbuilding copy limits
  local game = Game.new(seed or 1, "test", require("content.coin_order"), nil, false)
  Game.START_MAX = keep
  for i, id in ipairs(ids) do game.coins[i].id = id end
  local e = game.encounter
  e.quota, e.max_quota = 1000, 1000
  e.combo_step = 0
  e.queue, e.pile = {}, {}
  for _, coin in ipairs(game.coins) do e.queue[#e.queue + 1] = coin.uid end
  game.dealt = {uid = e.queue[1], probability = .5}
  require("src.hooks").bind(game, game.coins[1])
  game.player.energy = 99
  return game
end

local function flip(game, side)
  assert(Game.flip(game))
  game.pending.result = side
  assert(Game.resolve(game))
end

-- Double Down: effects without an amount (next_heads, next_swap) no longer crash; extra_draw is left alone
for _, id in ipairs({"domino", "mirror", "lucky", "hourglass"}) do
  local g = level({id, "normal", "normal"})
  g.items = {"double_down"}
  assert(Game.use_item(g, 1))
  flip(g, "Heads") -- must not raise
  local seen = {}
  for _, uid in ipairs(g.encounter.pile) do assert(not seen[uid], id .. ": a uid is in the pile twice") seen[uid] = true end
end

-- extra_draw never puts a coin into the pile twice
local g = level({"lucky", "normal", "normal"})
g.encounter.pile = {g.coins[1].uid}
flip(g, "Heads")
local n = 0
for _, uid in ipairs(g.encounter.pile) do if uid == g.coins[1].uid then n = n + 1 end end
equal(n, 1, "extra_draw guard")

-- Echo repeats Megaphone (effects with coins)
g = level({"megaphone", "echo", "normal"})
flip(g, "Heads")
flip(g, "Heads") -- must not raise

-- Fuse discarded from the opening hand is charged
g = Game.new(3, "blade", nil, nil, true)
local fuse = g.coins[1]
fuse.id = "fuse"
local hand = g.mulligan.hand
local target = fuse.uid
local inhand = false
for _, uid in ipairs(hand) do if uid == target then inhand = true end end
if not inhand then hand[1] = target end
equal(Game.mulligan_discard(g, {target}), 1)
equal(fuse.charge, 6, "mulligan discard charges Fuse")

-- Lucky Charm stacks with an earlier Weighted
g = level({"normal", "normal", "normal"})
g.items = {"weighted", "lucky_charm"}
assert(Game.use_item(g, 1))
equal(g.dealt.probability, .75)
assert(Game.use_item(g, 1))
assert(math.abs(g.dealt.probability - .95) < 1e-9, "weighted then lucky charm: " .. g.dealt.probability)

-- Martyr: Heads pays +1 per Tails flipped this level by any coin
g = level({"normal", "normal", "martyr", "normal"})
flip(g, "Tails")
flip(g, "Tails")
local before = g.encounter.scored
flip(g, "Heads")
equal(g.encounter.scored - before, 4 + 2, "martyr counts every Tails this level")

-- odds hooks apply in the encounter only (momentum shows base odds in the shop)
g = level({"momentum", "normal"})
g.encounter.streak = 3
local boosted = Game.probability(g, g.coins[1])
g.phase = "SHOP"
assert(Game.probability(g, g.coins[1]) < boosted, "shop shows base odds")
equal(Game.probability(g, g.coins[1]), Game.catalog().momentum.probability)

-- profile: string options survive a save/load
local p = Profile.new()
local d
p.options.language = "de"
equal(Profile.decode(Profile.encode(p)).options.language, "de", "language is saved")
equal(Profile.decode("return {tokens = 0, unlocked = {}, options = {language = de}}").options.language, "en", "old unquoted file still loads")

-- stages: a first win on stage 1 unlocks stage 2 (stake is recorded before the win, as in actions.lua)
p = Profile.new()
equal(Profile.record_stake_win(p, "blade", 1), 2, "first win on stage 1 unlocks stage 2")
Profile.record_win(p, "blade")
equal(Profile.max_stake(p, "blade"), 2)
equal(p.stakes.blade, 2, "and stores it")
local old = Profile.new()
old.wins.blade = true
equal(Profile.max_stake(old, "blade"), 2, "old profile starts at stage 2")

-- Broken Clock wins over the House inversion: flip 10 of a boss level (stage 1) and flip 20 (stage 4, inverts every 4th) stay Heads
for _, case in ipairs({{stake = 1, flips = 9}, {stake = 4, flips = 19}}) do
  g = level({"normal", "normal"})
  g.stake = case.stake
  g.relics = {"clock"}
  require("src.relics").bind(g)
  g.encounter.boss, g.encounter.inverts, g.encounter.flips = true, true, case.flips
  assert(Game.flip(g))
  equal(g.pending.result, "Heads", "clock beats the House on flip " .. case.flips + 1 .. " (stage " .. case.stake .. ")")
end

-- a saved run that names unknown content is rejected, not crashed on
local snap = require("src.serialize").decode(require("src.serialize").encode(Game.snapshot(Game.new(3, "blade", {}, nil, true, 1))))
local function restored(mutate)
  local d = require("src.serialize").decode(require("src.serialize").encode(snap))
  mutate(d)
  return Game.restore(d)
end
assert(restored(function() end), "a good snapshot restores")
assert(not restored(function(d) d.relics = {"gone"} end), "unknown relic")
assert(not restored(function(d) d.relics = nil end), "no relic list")
assert(not restored(function(d) d.coins[1].id = "gone" end), "unknown coin")
assert(not restored(function(d) d.items = {"gone"} end), "unknown item")

-- profile.decode drops or defaults damaged entries and never crashes
for _, text in ipairs({
  "return {tokens=1,unlocked={blade=5}}", "return {tokens=1,unlocked={},sets={blade=5}}", "return {tokens=1,unlocked={},sets={blade={5}}}",
  "return {tokens=1,unlocked={},sets={blade={{}}}}", "return {tokens=1,unlocked={},sets={blade={}}}", "return {tokens=1,unlocked={},active_set={blade='x'}}",
  "return {tokens=1,unlocked={},stakes={blade='x'}}", "return {tokens=1,unlocked={},best_endless={blade='x'}}", "return {tokens=1,unlocked={},sets={[1]={}}}",
  "return {tokens=1,unlocked={},stakes={[1]=1}}", "return {tokens=0/0,unlocked={}}", "return {tokens=-5,unlocked={}}", "return {tokens=1,unlocked={}", "", "return 5",
}) do
  local d = Profile.decode(text)
  for _, id in ipairs(Profile.CHARACTER_ORDER) do
    Profile.max_stake(d, id) Profile.sets(d, id) Profile.active(d, id) Profile.loadout(d, id, 5, 2)
    Profile.record_stake_win(d, id, 1) Profile.record_endless(d, id, 3) Profile.character_unlocked(d, id)
  end
  Profile.decode(Profile.encode(d))
end
d = Profile.decode("return {tokens=1,unlocked={},stakes={blade=99,seer=0},active_set={blade=7}}")
equal(d.stakes.blade, #require("content.stakes"), "stage clamped to the last")
equal(d.stakes.seer, 1, "stage clamped to 1")
equal(Profile.active(d, "blade"), 1, "active set out of range")
equal(Profile.decode("return {tokens=-5,unlocked={}}").tokens, 0, "negative tokens")
equal(#Profile.sets(Profile.decode("return {tokens=1,unlocked={},sets={blade={{name='a',coins={'zzz'}}}}}"), "blade")[1].coins, #require("content.characters").blade.deck, "unknown coin ids fall back to the default deck")

-- options: only known keys with the right type survive, and encode never writes unloadable text or raises
do
  local junk = Profile.decode("return {tokens=1,unlocked={},options={volume_master=500,volume_sfx=-3,volume_music=0/0,language='fr',screen_shake='yes',[1]=2,['a b']=1,evil={},[2.5]=1}}")
  local o = junk.options
  equal(o.volume_master, 100, "volume clamped") equal(o.volume_sfx, 0, "volume clamped low") equal(o.volume_music, 40, "NaN volume defaults")
  equal(o.language, "en", "unknown language") equal(o.screen_shake, true, "wrong type defaults") equal(o["a b"], nil, "unknown key dropped")
  local n = 0
  for _ in pairs(o) do n = n + 1 end
  equal(n, 8, "exactly the default options")
  local p = Profile.new()
  p.options.language = "de"
  p.options["a b"], p.options["end"], p.options[1], p.options.bad, p.options.nan, p.options.inf = 1, 1, 2, {}, 0/0, math.huge
  local text = Profile.encode(p)
  local back = Profile.decode(text)
  equal(back.options.language, "de", "valid options survive")
  assert(load(text, "profile", "t", {}), "encoded text always loads")
end

-- Game.restore: structural validation, then random damage never crashes a later action
do
  local Serialize = require("src.serialize")
  local function copy(t) if type(t) ~= "table" then return t end local o = {} for k, v in pairs(t) do o[k] = copy(v) end return o end
  local function paths(t, prefix, out)
    for k, v in pairs(t) do
      local p = {}
      for i = 1, #prefix do p[i] = prefix[i] end
      p[#p + 1] = k
      out[#out + 1] = p
      if type(v) == "table" then paths(v, p, out) end
    end
    return out
  end
  local hand = Game.new(3, "blade", require("content.coin_order"), nil, true, 1)
  Game.add_relic(hand, "metronome")
  local shop = Game.new(5, "seer", nil, nil, false, 2)
  shop.encounter.quota, shop.encounter.max_quota, shop.player.gold, shop.phase = 1, 1, 99, "SHOP"
  shop.shop_offers, shop.items, shop.shop_items = {"dagger", false, "hammer", "focus"}, {"peek"}, {"peek", false}
  local values = {0 / 0, math.huge, -1, "x", true, false, {}, {1}, {"zzz"}, {[1] = 1, [3] = 3}, 0, 2.5, 99999}
  local seen = 0
  for _, base in ipairs({Serialize.decode(Serialize.encode(Game.snapshot(hand))), Serialize.decode(Serialize.encode(Game.snapshot(shop)))}) do
    for _, path in ipairs(paths(base, {}, {})) do
      if path[1] ~= "log" then
        for _, value in ipairs(values) do
          local d = copy(base)
          local target = d
          for i = 1, #path - 1 do target = target[path[i]] end
          target[path[#path]] = value
          local g = Game.restore(d)
          if g then
            seen = seen + 1
            for i = 1, 12 do
              local ok, err = pcall(function()
                for _, c in ipairs(g.coins) do Game.probability(g, c) end
                if g.mulligan then Game.mulligan_done(g)
                elseif g.phase == "SHOP" then Game.buy(g, 1 + i % 4) Game.buy_slot(g) Game.leave_shop(g)
                elseif g.pending then Game.resolve(g)
                elseif g.dealt then if i % 3 == 0 then Game.discard(g) else Game.flip(g) end
                else Game.flip(g) end
              end)
              assert(ok, "damaged save raised (" .. table.concat(path, ".", 1, #path) .. " = " .. tostring(value) .. "): " .. tostring(err))
            end
          end
        end
      end
    end
  end
  assert(seen > 0, "harmless mutations still restore")
  assert(not restored(function(d) d.player.gold = 0 / 0 end), "NaN gold")
  assert(not restored(function(d) d.player.gold = -5 end), "negative gold")
  assert(not restored(function(d) d.coins[2].uid = d.coins[1].uid end), "duplicate uid")
  assert(not restored(function(d) d.coins[1].uid = "a" end), "non-numeric uid")
  assert(not restored(function(d) d.encounter.queue = {999} end), "queue names a missing coin")
  assert(not restored(function(d) d.mulligan.hand = {999} end), "hand names a missing coin")
  assert(not restored(function(d) d.encounter = nil end), "no encounter")
  assert(not restored(function(d) d.shop_offers = nil end), "no shop offers")
  assert(not restored(function(d) d.rng_state = 0 end), "rng state out of range")
end

-- non-finite seeds are normalised like other invalid ones
equal(require("src.rng").seed(0 / 0), 1, "NaN seed")
equal(require("src.rng").seed(math.huge), 1, "inf seed")
equal(Game.new(-math.huge, "blade").seed, 1, "Game.new with an infinite seed")

-- Sound.watch: the first look at a game (new or resumed) plays nothing; later changes still do
do
  local Sound = require("src.ui.sound")
  local played = {}
  local real = Sound.play
  Sound.play = function(name) played[#played + 1] = name end
  local g = Game.new(4, "blade", nil, nil, false, 1)
  g.last_result = {gained = 5, combo = {len = 3}}
  g.phase = "SHOP"
  local ui = {game = g}
  Sound.last = {}
  Sound.watch(ui)
  equal(#played, 0, "resuming a shop run is silent")
  g.last_result = {gained = 5, combo = {len = 3}}
  Sound.watch(ui)
  assert(#played >= 2, "a new result still plays score and combo")
  played = {}
  g.phase = "ENCOUNTER"
  Sound.watch(ui)
  g.phase = "SHOP"
  Sound.watch(ui)
  equal(played[#played], "shop", "phase changes still play")
  ui.game = nil
  Sound.watch(ui)
  Sound.last = {}
  Sound.play = real
end

-- a genuine run must always restore: the House's opening hand (no payout) and a shop after a coin was removed (stale uids in the old level)
do
  local Serialize = require("src.serialize")
  local function roundtrip(game) return Game.restore(Serialize.decode(Serialize.encode(Game.snapshot(game)))) end
  local g = Game.new(3, "blade", nil, nil, true)
  local guard = 0
  while g.encounter_index < 4 and guard < 500 do
    guard = guard + 1
    if g.mulligan then Game.mulligan_done(g)
    elseif g.phase == "ENCOUNTER" and g.dealt then g.player.energy = 99 Game.flip(g) Game.resolve(g)
    elseif g.phase == "ENCOUNTER" and g.encounter.cleared then Game.end_level(g)
    elseif g.phase == "ENCOUNTER" and Game.can_exchange(g) then Game.exchange(g)
    elseif g.phase == "SHOP" then Game.leave_shop(g)
    else break end
  end
  assert(g.mulligan and g.encounter.name == "The House", "reached the boss opening hand")
  assert(roundtrip(g), "boss opening hand restores")
  local shop = Game.new(1, "blade", nil, nil, false)
  shop.encounter.quota = 0 -- make shop entry deterministic across balance changes
  while shop.dealt do shop.player.energy = 99 Game.flip(shop) Game.resolve(shop) end
  assert(Game.end_level(shop))
  shop.player.gold = 50
  assert(Game.remove(shop, shop.encounter.played[1]), "removed a played coin")
  assert(roundtrip(shop), "shop after removing a played coin restores")
end

print("bugfix tests passed")
