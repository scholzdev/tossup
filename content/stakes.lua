-- Stages (difficulty levels), chosen per run on the play screen. A character unlocks the next stage by winning a run on
-- the highest one it has. Every stage keeps the rules of the ones below it; a later stage may overwrite a value.
-- Rules read by src/game.lua: quota_mult, start_gold, boss_every (the House / endless inversion), price_mult
-- (coins, chips and prizes in the shop), exchange_max, modifiers_from (first level with a level modifier).
-- `text` is a Lang key (uppercase, shown on the play screen).
return {
  {text = "NO CHANGES"},
  {text = "QUOTAS +15%", rules = {quota_mult = 1.15}},
  {text = "START WITH 15 GOLD", rules = {start_gold = 15}},
  {text = "THE HOUSE INVERTS EVERY 4TH FLIP", rules = {boss_every = 4}},
  {text = "SHOP PRICES +20%", rules = {price_mult = 1.2}},
  {text = "ONLY 2 EXCHANGES PER LEVEL", rules = {exchange_max = 2}},
  {text = "LEVEL 1 HAS A MODIFIER TOO", rules = {modifiers_from = 1}},
  {text = "QUOTAS +50% IN TOTAL", rules = {quota_mult = 1.5}},
}
