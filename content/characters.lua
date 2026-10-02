-- deck: the starting coin set a new player gets (up to 5 coins). pool: coins you can use in coin sets from the
-- start. locked: {id, _} coins that are unlocked by buying them in the shop (the second value is
-- unused). The shop sells every coin of a character, pool and locked alike.
return {
  blade = {
    name = "The Blade", description = "Reliable points",
    starter = "normal",
    deck = {"normal", "sword", "dagger"},
    pool = {"normal", "sword", "dagger"},
    locked = {{"hammer", 3}, {"blood", 4}, {"vampire", 4}, {"chain", 5}, {"cursed", 5}, {"fuse", 5},
      {"focus", 6}, {"martyr", 6}, {"snowball", 8}, {"spark", 3}, {"jackpot", 5}, {"lifeline", 4},
      {"megaphone", 5}, {"pot", 4},
      {"hot_hand", 4}, {"cash_out", 5},
      {"doubler", 6}, {"amplifier", 6}},
  },
  seer = {
    name = "The Seer", description = "Risk and changing odds",
    starter = "normal",
    deck = {"normal", "normal", "dagger", "focus", "spark"},
    pool = {"normal", "dagger", "cursed", "gambler", "spark", "focus", "lucky"},
    locked = {{"horoscope", 4}, {"crystal_ball", 5}, {"mimic", 6}, {"contrarian", 4}, {"lucky_seven", 4}, {"blood", 4},
      {"hourglass", 5}, {"jester", 5}, {"echo", 6}, {"phoenix", 6},
      {"mirror", 5}, {"domino", 6}, {"twin", 5},
      {"cold_streak", 4}, {"anchor", 4},
      {"true_echo", 6}, {"amplifier", 6}},
  },
  trader = {
    name = "The Trader", description = "Gold and energy",
    starter = "normal",
    deck = {"normal", "loaded", "dagger"},
    pool = {"normal", "copper", "loaded", "dagger", "sword"},
    locked = {{"spark", 3}, {"bank", 4}, {"miser", 4}, {"hammer", 4}, {"bounty", 5},
      {"flock", 5}, {"momentum", 5}, {"capacitor", 6},
      {"cheerleader", 4}, {"megaphone", 5}, {"orchestra", 4}, {"lifeline", 4}, {"jackpot", 5},
      {"bettor", 5}, {"anchor", 4},
      {"doubler", 6}, {"true_echo", 6}},
  },
}
