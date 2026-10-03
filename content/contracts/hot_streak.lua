return {
  id = "hot_streak",
  name = "HOT STREAK",
  description = "Reach a combo of 4 before clearing the quota.",
  drawback = "A Tie breaks your combo and loses its unbanked pot.",
  reward = 5,
  tie_breaks_combo = true,
  complete = function(encounter) return (encounter.best_combo_len or 0) >= 4 end,
}
