local app = require("src.ui.app")

love.load = app.load
love.draw = app.draw
love.update = app.update
love.mousepressed = app.mousepressed
love.keypressed = app.keypressed
