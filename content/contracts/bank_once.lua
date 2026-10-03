return {
  id = "bank_once",
  name = "LOCK IT IN",
  description = "Bank a combo before clearing the quota.",
  drawback = "Your combo multiplier is capped at x2.",
  reward = 3,
  combo_multiplier_cap = 2,
  complete = function(encounter) return encounter.combo_banked end,
}
