-- deck: the starting coin set a new player gets (5 coins, leaving 5 free slots). pool: coins you can use in coin sets from the
-- start. locked: {id, _} coins that are unlocked by buying them in the shop (the second value is
-- unused). The shop sells every coin of a character, pool and locked alike.
return {
  blade = {
    name = "The Blade", description = "Reliable points",
    starter = "normal",
    deck = {"normal", "normal", "normal", "normal", "sword"},
    pool = {"normal", "sword", "dagger"},
    locked = {{"hammer", 3}, {"blood", 4}, {"vampire", 4}, {"chain", 5}, {"cursed", 5}, {"fuse", 5},
      {"focus", 6}, {"martyr", 6}, {"snowball", 8}},
  },
  seer = {
    name = "The Seer", description = "Risk and changing odds",
    starter = "normal",
    deck = {"normal", "normal", "normal", "focus", "spark"},
    pool = {"normal", "cursed", "gambler", "spark", "focus", "lucky"},
    locked = {{"dagger", 3}, {"contrarian", 4}, {"lucky_seven", 4}, {"blood", 4},
      {"hourglass", 5}, {"jester", 5}, {"echo", 6}, {"phoenix", 6}},
  },
  trader = {
    name = "The Trader", description = "Gold and energy",
    starter = "normal",
    deck = {"normal", "normal", "normal", "loaded", "dagger"},
    pool = {"normal", "copper", "loaded", "dagger", "sword"},
    locked = {{"spark", 3}, {"bank", 4}, {"miser", 4}, {"hammer", 4}, {"bounty", 5},
      {"flock", 5}, {"momentum", 5}, {"capacitor", 6}},
  },
}
