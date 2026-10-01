return {
  name = "Lucky Charm", short = "CHARM", cost = 10,
  description = "The next 2 coins +20% Heads",
  use = function(game)
    local Game = require("src.game")
    Game.add_buff(game, "odds", .2, 2, true) -- active from the coin in play
    game.dealt.probability = Game.probability(game, Game.get_coin(game, game.dealt.uid))
  end,
}
