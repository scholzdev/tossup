local Game = require("src.game")
local Profile = require("src.profile")
local ui = require("src.ui.state")
local A = require("src.ui.actions")

local files, writes = { ["run.lua"] = "normal save" }, {}
love = {
  window = {setFullscreen = function() end},
  filesystem = {
    getInfo = function(path) return files[path] and {} or nil end,
    read = function(path) return files[path] end,
    write = function(path, value) files[path] = value writes[#writes + 1] = path end,
  },
  timer = {getTime = function() return 0 end},
}

A.load_profile()
assert(Game.DEV_MODE == (os.getenv("TOSSUP_DEV") == "1"))
assert(A.has_saved_run() == not Game.DEV_MODE, "development has a separate run save")

A.go("select")
A.open_sets("blade")
assert(ui.screen == "sets")
A.back_from_sets()
assert(ui.screen == "select", "Coin Sets returns to run setup")
A.go("title")
A.open_sets("blade")
A.back_from_sets()
assert(ui.screen == "title", "Coin Sets returns to the title screen when opened there")

if Game.DEV_MODE then
  for _, character_id in ipairs(Profile.CHARACTER_ORDER) do
    assert(Profile.character_unlocked(ui.profile, character_id))
    assert(Profile.max_stake(ui.profile, character_id) == #Game.stakes())
    for _, entry in ipairs(Game.characters()[character_id].locked) do
      assert(Profile.is_unlocked(ui.profile, character_id, entry[1]))
    end
  end
  for id in pairs(Game.catalog()) do assert(ui.profile.collected[id]) end
  assert(ui.profile.options.seen_help)
  ui.selected_character = "trader"
else
  assert(not Profile.character_unlocked(ui.profile, "trader"))
  assert(not Profile.is_unlocked(ui.profile, "blade", "hammer"))
  ui.selected_character = "blade"
end

A.start(123)
assert(ui.game and ui.game.player.gold == (Game.DEV_MODE and 5000 or Game.START_GOLD))
A.toggle_option("screen_shake")
assert((#writes == 0) == Game.DEV_MODE, "development never writes unlocks to the normal profile")
print("dev mode tests passed")
