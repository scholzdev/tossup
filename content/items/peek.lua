return {
  name = "Peek", short = "PEEK", cost = 6,
  description = "See the next two coins",
  use = function(game)
    local peek = {}
    for i = 1, 2 do
      local uid = game.encounter.pile[i]
      if uid then peek[#peek + 1] = uid end
    end
    if #peek == 0 then return false end
    game.peek = peek
  end,
}
