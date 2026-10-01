-- Level modifiers: from level 2 on every level gets one (picked with the seeded RNG). apply(game, e) changes the
-- freshly built encounter `e`. Names and descriptions are translated through locales/<lang>.lua (modifiers table).
return {
  lucky_day = {name = "Lucky Day", description = "All coins +10% Heads.",
    apply = function(_, e) e.magnet = e.magnet + .10 end},
  cold_snap = {name = "Cold Snap", description = "All coins -10% Heads, but the payout is 40% higher.",
    apply = function(_, e) e.magnet = e.magnet - .10 e.payout = math.floor((e.payout or 0) * 1.4 + .5) end},
  power_surge = {name = "Power Surge", description = "You start with 5 energy.",
    apply = function(game) game.player.energy = 5 end},
  blackout = {name = "Blackout", description = "You start with 1 energy, but the payout is 40% higher.",
    apply = function(game, e) game.player.energy = 1 e.payout = math.floor((e.payout or 0) * 1.4 + .5) end},
  gold_rush = {name = "Gold Rush", description = "Gold effects pay double.",
    apply = function(_, e) e.gold_mult = 2 end},
  high_stakes = {name = "High Stakes", description = "Quota +25%, payout +50%.",
    apply = function(_, e)
      e.quota = math.floor(e.quota * 1.25 + .5)
      e.max_quota = e.quota
      e.payout = math.floor((e.payout or 0) * 1.5 + .5)
    end},
  good_rhythm = {name = "Good Rhythm", description = "The combo grows +0.4 per step.",
    apply = function(_, e) e.combo_step = .4 end},
  bonus_exchange = {name = "Bonus Exchange", description = "One extra exchange this level.",
    apply = function(_, e) e.extra_exchanges = (e.extra_exchanges or 0) + 1 end},
}
