local app = require("src.ui.app")

-- Write crashes to crash.log in the save folder (next to runs.log) before LÖVE shows its error screen.
local default_errorhandler = love.errorhandler
function love.errorhandler(message)
  pcall(function()
    love.filesystem.append("crash.log", os.date("%Y-%m-%d %H:%M:%S") .. " v" .. require("src.version").number .. "-" .. require("src.version").build .. "\n" .. debug.traceback(tostring(message), 2) .. "\n\n")
  end)
  return default_errorhandler(message)
end

love.load = app.load
love.draw = app.draw
love.update = app.update
love.mousepressed = app.mousepressed
love.mousemoved = app.mousemoved
love.gamepadpressed = app.gamepadpressed
love.mousereleased = app.mousereleased
love.keypressed = app.keypressed
