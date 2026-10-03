return {
  id = "clean_run",
  name = "CLEAN RUN",
  description = "Clear the quota without discarding a coin.",
  drawback = "Each Tails increases the quota by 2.",
  reward = 4,
  tails_quota_penalty = 2,
  complete = function(encounter) return encounter.discards == 0 end,
}
