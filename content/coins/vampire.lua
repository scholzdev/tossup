-- Hook: on_resolve turns a side effect into a cross-stat trade (points for gold).
return {
  name = "Vampire", description = "Heads: 2 points and it drains 2 gold from the house.",
  probability = .5,
  heads = {{type = "score", amount = 2}}, tails = {},
  on_resolve = function(_, _, res)
    if res.result == "Heads" then res.effects[#res.effects + 1] = {type = "gold", amount = 2} end
  end,
}
