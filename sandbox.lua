-- Edit this scene, then run `SANDBOX=1 ./tools/run.sh`. Press F5 to reload it.
-- To load another file directly: `./tools/run.sh scenes/shop.lua`.
-- Sandbox runs use a temporary profile and never overwrite the normal saved run.
return {
  screen = "encounter", -- encounter | shop | title | select | collection | sets | options | help
  character = "blade",
  coins = {"blood", },
  -- odds = {blood = {heads = .01, tie = .80}}, -- optional: otherwise use Blood's normal odds
  energy = 99,
  gold = 50,
}
