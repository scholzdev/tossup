# Rework Level Design

We currently have
```lua
local route = {
  {name = "Opening", per_coin = 0.9, payout = 25},
  {name = "Second Chance", per_coin = 1.4, payout = 30},
  {name = "High Stakes", per_coin = 2.5, payout = 35},
  {name = "The House", per_coin = 4.5, boss = true},
}
```

which sets a predetermined scaling per level.

I'd love to see another scaling an a totally different architecture, ie:
