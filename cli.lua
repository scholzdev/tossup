local Game = require("src.game")
local seed = tonumber(arg and arg[1]) or 381927
local game = Game.new(seed, arg and arg[2])
local printed = 0

local function print_new()
  for i = printed + 1, #game.log do print(game.log[i]) end
  printed = #game.log
end

print_new()
while game.phase ~= "VICTORY" and game.phase ~= "GAME_OVER" do
  if game.phase == "ENCOUNTER" then
    Game.flip(game)
    Game.resolve(game)
  elseif game.phase == "SHOP" then
    Game.leave_shop(game)
  end
  print_new()
end
print("Result: " .. game.phase .. " | Cleared " .. game.cleared .. " | Gold " .. game.player.gold)
