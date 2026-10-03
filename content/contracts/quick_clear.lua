return {
  id = "quick_clear",
  name = "QUICK CLEAR",
  description = "Clear the quota within 6 flips.",
  drawback = "Heads odds are reduced by 5 percentage points.",
  reward = 5,
  heads_penalty = .05,
  complete = function(encounter) return encounter.flips <= 6 end,
}
