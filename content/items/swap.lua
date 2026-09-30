return {
  name = "Swap", short = "SWAP", cost = 8,
  description = "Free discard, new coin",
  use = function(game)
    return require("src.game").discard(game, true) -- lazy: src.game loads this file
  end,
}
