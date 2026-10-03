return {
  id = "thin_market",
  name = "Thin Market",
  description = "Shops show one fewer coin, but every coin costs 2 fewer gold.",
  on_trigger = function(ctx)
    if ctx.event == "run_start" then
      ctx.game.shop.coin_offer_count = 3
      ctx.game.shop.coin_price_discount = 2
    end
  end,
}
