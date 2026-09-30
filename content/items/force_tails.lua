return {
  name = "Force Tails", short = "FORCE T", cost = 12,
  description = "Dealt coin lands Tails",
  use = function(_, Items)
    Items.arm("coin_flip", function(e) e.flip.result = "Tails" e.flip.forced = true end)
  end,
}
