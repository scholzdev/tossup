-- Sets res.cash_out: the game squares the combo multiplier for this flip and then resets the combo.
return {
  name = "Cash Out", description = "Heads: 3 points, the combo multiplier counts twice, then the combo resets.",
  rarity = "SR",
  probability = .3,
  heads = {{type = "score", amount = 3}}, tails = {},
  on_resolve = function(_, _, res)
    if res.result == "Heads" then res.cash_out = true end
  end,
}
