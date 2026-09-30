return {
  name = "Force Heads", short = "FORCE H", cost = 12,
  description = "Dealt coin lands Heads",
  use = function(_, Items)
    Items.arm("coin_flip", function(e) e.flip.result = "Heads" e.flip.forced = true end)
  end,
}
