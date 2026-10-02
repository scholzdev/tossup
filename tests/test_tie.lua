local Game = require("src.game")
local scene = require("scenes.blood")

local function demo(seed)
  local cfg = {seed = seed, character = scene.character, coins = scene.coins, odds = scene.odds,
    energy = scene.energy, gold = scene.gold}
  return Game.new_sandbox(cfg)
end

local blood = Game.catalog().blood
assert(blood.probability == .37 and blood.tie_probability == .09, "regular Blood keeps its own odds")
local normal = Game.new_sandbox({coins = {"blood"}})
assert(normal.dealt.probability == blood.probability and normal.dealt.tie_probability == blood.tie_probability,
  "coins without an odds override keep their printed odds")
assert(Game.new_sandbox({coins = {"blood"}}).seed ~= normal.seed, "seedless scenes get a new roll on reload")
local g = demo(1)
assert(#g.coins == 5 and g.slots == 5 and g.player.energy == 99)
for _, owned in ipairs(g.coins) do assert(owned.id == "blood") end
assert(g.dealt.probability == .10 and g.dealt.tie_probability == .80, "sandbox odds are visible before flipping")
local boosted = demo(3)
boosted.items = {"weighted", "lucky_charm"}
assert(Game.use_item(boosted, 1))
assert(math.abs(boosted.dealt.probability - .20) < 1e-9, "Heads boosts cannot consume Edge chance")
assert(Game.use_item(boosted, 1))
assert(math.abs(boosted.dealt.probability + boosted.dealt.tie_probability - 1) < 1e-9)

-- The scene is seeded, but 80% Edge must occur often enough to exercise the animation by hand.
local edges = 0
for seed = 1, 100 do
  local run = demo(seed)
  assert(Game.flip(run))
  if run.pending.raw == "Tie" then edges = edges + 1 end
end
assert(edges >= 65 and edges <= 95, "80% Edge demo should visibly produce many Edges: " .. edges)

-- Edge applies exactly half of each printed side and preserves the existing streak.
g.encounter.quota, g.encounter.max_quota = 100, 100
assert(Game.flip(g))
g.pending.result = "Tie"
assert(Game.resolve(g))
assert(g.last_result.final == "Tie" and g.last_result.gained == 5 and g.last_result.penalty == 3)
assert(g.encounter.quota == 98 and g.encounter.max_quota == 103)
assert(g.encounter.combo_len == 0 and g.encounter.combo_side == nil)

g.encounter.combo_side, g.encounter.combo_len, g.encounter.streak, g.encounter.shield = "Heads", 2, 2, 1
assert(Game.flip(g))
g.pending.result = "Tie"
assert(Game.resolve(g))
assert(g.encounter.combo_side == "Heads" and g.encounter.combo_len == 2 and g.encounter.streak == 2)
assert(g.encounter.shield == 1, "Edge does not consume an Anchor shield")
assert(g.last_result.base_effects[1].amount == 3 and g.last_result.base_effects[2].amount == 5)

local opening = demo(scene.seed)
assert(Game.flip(opening) and opening.pending.result == "Tie")
assert(Game.resolve(opening) and opening.encounter.quota == 2, "Edge makes two points of quota progress")
assert(Game.flip(opening) and opening.pending.result == "Tie")
assert(Game.resolve(opening) and opening.encounter.cleared, "two Edges clear the opening quota")

-- The House inverts Heads/Tails, but an Edge remains an Edge.
local boss_edge
for seed = 1, 100 do
  local run = demo(seed)
  run.encounter.boss, run.encounter.flips = true, 4
  assert(Game.flip(run))
  if run.pending.raw == "Tie" then boss_edge = run break end
end
assert(boss_edge and boss_edge.pending.result == "Tie")

local shop = demo(2)
shop.encounter.cleared = true
assert(Game.end_level(shop))
assert(#shop.shop_offers == 1 and shop.shop_offers[1] == "blood", "demo shop offers only Blood")

if Game.SANDBOX_MODE then
  local files, writes = {["profile.lua"] = "normal profile", ["run.lua"] = "normal run"}, 0
  love = {
    window = {setFullscreen = function() end},
    filesystem = {
      getInfo = function(path) return files[path] and {} or nil end,
      read = function(path) return files[path] end,
      write = function() writes = writes + 1 end,
    },
  }
  local A = require("src.ui.actions")
  local ui = require("src.ui.state")
  A.load_profile()
  assert(not A.has_saved_run(), "sandbox ignores the normal saved run")
  assert(A.start_sandbox("scenes/blood.lua") and ui.game.sandbox and #ui.game.coins == 5)
  assert(ui.game.phase == "ENCOUNTER" and not ui.game.paused)
  A.toggle_option("screen_shake")
  assert(writes == 0, "sandbox does not write the normal profile")
  assert(A.start() and ui.game.coins[1].id == "blood", "new run reloads the sandbox scene")

  assert(A.start_sandbox("scenes/shop.lua"), "a project-relative scene file loads")
  assert(ui.game.phase == "SHOP" and ui.game.player.gold == 50)
  local selected_scene = ui.game
  assert(not A.start_sandbox("../sandbox.lua") and ui.game == selected_scene, "scene path stays project-relative")

  -- F5 rereads the scene and rebuilds the selected screen from scratch.
  local app = require("src.ui.app")
  local config = scene
  package.preload.sandbox = function() return config end
  config = {screen = "shop", seed = 7, character = "blade", coins = {"blood"}, gold = 80}
  app.keypressed("f5")
  assert(ui.game.phase == "SHOP" and not ui.game.paused and ui.game.player.gold == 80)
  assert(#ui.game.shop_offers == 1 and ui.game.shop_offers[1] == "blood")
  local old_shop = ui.game
  app.keypressed("f5")
  assert(ui.game ~= old_shop and ui.game.phase == "SHOP", "F5 rebuilds the shop")

  for _, screen in ipairs({"title", "select", "collection", "sets", "options", "help"}) do
    config = {screen = screen, seed = 7, character = "blade", coins = {"blood"}}
    app.keypressed("f5")
    assert(ui.screen == screen and ui.game.paused and ui.game.phase == "ENCOUNTER", "opens " .. screen)
  end
  local previous = ui.game
  config = {screen = "unknown", coins = {"blood"}}
  assert(not A.start_sandbox() and ui.game == previous and ui.screen == "help", "invalid screen preserves scene")
  package.preload.sandbox = nil
  package.loaded.sandbox = nil
end

print("tie demo tests passed")
