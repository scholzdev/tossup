-- Stable Blood/Edge fixture for the tie demo test. Also playable via `./tools/run.sh scenes/blood.lua`.
return {
  screen = "encounter",
  seed = 6,
  character = "blade",
  coins = {"blood", "blood", "blood", "blood", "blood"},
  odds = {blood = {heads = .10, tie = .80}},
  energy = 99,
  gold = 50,
}
