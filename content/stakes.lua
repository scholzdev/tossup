-- Stages (difficulty levels), chosen per run on the play screen. A character unlocks the next stage by winning a run on
-- the highest one it has. Every stage keeps the rules of the ones below it; a later stage may overwrite a value.
-- Rules read by src/game.lua: quota_mult, start_gold, boss_every (the House / endless inversion), price_mult
-- (coins, chips and prizes in the shop), exchange_max, modifiers_from (first level with a level modifier).
-- `text` is a Lang key (uppercase, shown on the play screen); `info` is the same rule as a plain sentence (wiki).
return {
  {info = "No changes", text = "NO CHANGES"},
  {info = "Quotas +15%", text = "QUOTAS +15%", rules = {quota_mult = 1.15}},
  {info = "Start with 15 gold", text = "START WITH 15 GOLD", rules = {start_gold = 15}},
  {info = "The House inverts every 4th flip", text = "THE HOUSE INVERTS EVERY 4TH FLIP", rules = {boss_every = 4}},
  {info = "Shop prices +20%", text = "SHOP PRICES +20%", rules = {price_mult = 1.2}},
  {info = "Only 2 exchanges per level", text = "ONLY 2 EXCHANGES PER LEVEL", rules = {exchange_max = 2}},
  {info = "Level 1 has a modifier too", text = "LEVEL 1 HAS A MODIFIER TOO", rules = {modifiers_from = 1}},
  {info = "Quotas +80% in total", text = "QUOTAS +80% IN TOTAL", rules = {quota_mult = 1.8}},
}
