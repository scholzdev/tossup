function love.conf(t)
  t.identity = "tossup"
  t.window.title = "Tossup"
  t.window.width = 1280
  t.window.height = 800
  t.window.resizable = true -- the 1280x800 canvas scales and centres itself in any window size
  t.window.minwidth = 640
  t.window.minheight = 400
  t.window.icon = "assets/ui/icon.png"
end
