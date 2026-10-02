return {
  name = "Weighted", short = "WEIGHT", cost = 8,
  description = "+25% Heads, one flip",
  use = function(game)
    game.dealt.probability = math.min(1 - (game.dealt.tie_probability or 0), game.dealt.probability + .25)
  end,
}
