using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    // Shop (full screen): coins, chips (items) and prizes (relics), a reroll for the coin offers, and
    // tune-ups for the coin you select in the deck strip. Buying a locked coin unlocks it for good.
    public static class ShopView
    {
        static readonly Rgba ScreenColor = new Rgba(.09f, .27f, .30f);
        static readonly Rgba PanelColor = new Rgba(.06f, .20f, .23f);

        // Grid: offer columns start at X0 with a fixed step, so every row lines up with the coin row.
        const float X0 = 340, Step = 150;

        static void VerticalLabel(string word, float x, float y)
        {
            var chars = Lang.Chars(L(word));
            for (int i = 0; i < chars.Count; i++)
                Centered(i == 0 ? Lang.Upper(chars[i]) : chars[i], x, y + i * 30, 30, Ui.F32, C.White);
        }

        static void Price(int amount, float x, float y, float w, bool affordable) =>
            Centered(amount.ToString(), x, y, w, Ui.F32, affordable ? C.Gold : C.Red);

        public static void Draw()
        {
            var g = Ui.Game;
            Box(0, 0, 1280, 800, C.FeltDark);
            Box(36, 36, 1208, 728, ScreenColor);
            Outline(36, 36, 1208, 728, C.Gold);

            // title, gold, menu
            Color(C.White);
            var title = Title("shop");
            Gfx.Draw(title, 70, 44, 110f / title.Height, 110f / title.Height);
            Button("MENU", 1120, 56, 100, 34, C.PanelLight, A.OpenMenu);
            ImageAt(Ui.UiImages["gold"], 1010, 100, 44);
            Text(GameText.Num(g.Player.Gold), 1062, 104, Ui.F32, C.Gold);

            // reroll (coin offers only), level with the coin icons
            int rerollCost = g.RerollCost;
            Box(70, 228, 190, 140, PanelColor);
            Outline(70, 228, 190, 140, C.PanelLight);
            ImageAt(Ui.UiImages["reroll"], 82, 238, 34);
            Text("REROLL", 124, 245, Ui.F20, C.Muted);
            Price(rerollCost, 70, 274, 190, g.Player.Gold >= rerollCost);
            Button("REROLL", 90, 322, 150, 36, C.Orange, () => Game.RerollShop(g), g.Player.Gold >= rerollCost);

            // COIN row
            bool full = g.Coins.Count >= Game.DeckMax;
            VerticalLabel("Coin", 290, 232);
            for (int i = 0; i < 4; i++)
            {
                float x = X0 + i * Step;
                string id = i < g.ShopOffers.Count ? g.ShopOffers[i] : null;
                if (id != null)
                {
                    int cost = Game.CoinCost(id);
                    Price(cost, x, 186, 110, g.Player.Gold >= cost && !full);
                    CoinImage(id, x + 7, 228, 96);
                    CoinHover(id, x, 228, 110, 96);
                    int index = i;
                    Button(full ? "FULL" : "BUY", x, 336, 110, 34, C.Blue, () => Game.Buy(g, index), g.Player.Gold >= cost && !full);
                }
                else Centered("SOLD", x, 268, 110, Ui.F32, C.Muted);
            }

            // CHIP row (items), columns 1-2
            VerticalLabel("Chip", 290, 436);
            for (int i = 0; i < 2; i++)
            {
                float x = X0 + i * Step;
                string id = i < g.ShopItems.Count ? g.ShopItems[i] : null;
                if (id != null)
                {
                    var def = Content.Items[id];
                    Price(def.Cost, x, 392, 110, g.Player.Gold >= def.Cost && g.Items.Count < Items.Max);
                    ImageAt(Ui.ItemImages[id], x + 7, 434, 96);
                    TextHover(Lang.ItemName(id), Lang.ItemDescription(id), x + 7, 434, 96, 96);
                    int index = i;
                    Button("BUY", x, 542, 110, 34, C.Blue, () => Game.BuyItem(g, index), g.Player.Gold >= def.Cost && g.Items.Count < Items.Max);
                }
                else Centered("SOLD", x, 474, 110, Ui.F32, C.Muted);
            }

            // PRIZE row (relic), column 4 so it lines up with the last coin offer
            float px = X0 + 3 * Step;
            VerticalLabel("Prize", px - 50, 396);
            if (g.ShopRelic != null)
            {
                Price(Game.RelicCost, px, 392, 110, g.Player.Gold >= Game.RelicCost);
                ImageAt(Ui.RelicImages[g.ShopRelic], px + 7, 434, 96);
                TextHover(Lang.RelicName(g.ShopRelic), Lang.RelicDescription(g.ShopRelic), px + 7, 434, 96, 96);
                Button("BUY", px, 542, 110, 34, C.Blue, () => Game.BuyRelic(g), g.Player.Gold >= Game.RelicCost);
            }
            else Centered("SOLD", px, 474, 110, Ui.F32, C.Muted);

            // tune-ups for the selected coin
            Box(1000, 190, 220, 370, PanelColor);
            Outline(1000, 190, 220, 370, C.Line);
            Centered("TUNE-UPS", 1000, 200, 220, Ui.F20, C.Gold);
            var selected = g.SelectedUid.HasValue ? Game.GetCoin(g, g.SelectedUid.Value) : null;
            Centered("ODDS TUNER", 1000, 236, 220, Ui.F20, C.Face);
            Centered("+10% HEADS ON THE", 1000, 262, 220, Ui.F16, C.Muted);
            Centered("SELECTED COIN", 1000, 281, 220, Ui.F16, C.Muted);
            Price(10, 1000, 298, 220, g.Player.Gold >= 10);
            Button("UPGRADE", 1032, 340, 156, 36, C.Gold, () => { if (g.SelectedUid.HasValue) Game.Upgrade(g, g.SelectedUid.Value); },
                g.Player.Gold >= 10 && selected != null && Game.Probability(g, selected) < 1);
            Centered("COIN REMOVAL", 1000, 400, 220, Ui.F20, C.Face);
            Centered("DROP THE SELECTED COIN", 1000, 426, 220, Ui.F16, C.Muted);
            Price(8, 1000, 444, 220, g.Player.Gold >= 8);
            Button("REMOVE", 1032, 486, 156, 36, C.Red, () => { if (g.SelectedUid.HasValue) Game.Remove(g, g.SelectedUid.Value); },
                g.Player.Gold >= 8 && g.Coins.Count > 1);

            // your deck
            Text(L("YOUR DECK  %d / %d", g.Coins.Count, Game.DeckMax), 70, 612, Ui.F20, C.Gold);
            Text(full ? "DECK FULL  -  REMOVE A COIN TO BUY ANOTHER" : "CLICK A COIN TO SELECT IT", 340, 618, Ui.F16, full ? C.Orange : C.Muted);
            var held = new List<string>();
            foreach (var id in g.Items) held.Add(Lang.ItemShort(id));
            foreach (var id in g.Relics) held.Add(Lang.Upper(Lang.RelicName(id)));
            if (held.Count > 0) Text(L("HELD  %s", string.Join(", ", held)), 340, 638, Ui.F16, C.Orange);
            for (int i = 0; i < Game.DeckMax; i++)
            {
                float x = 70 + i * 92;
                var item = i < g.Coins.Count ? g.Coins[i] : null;
                bool chosen = item != null && item.Uid == g.SelectedUid;
                Box(x, 664, 84, 76, item != null ? PanelColor : C.Slot);
                Outline(x, 664, 84, 76, chosen ? C.Orange : C.PanelLight);
                if (item == null) continue;
                CoinImage(item.Id, x + 18, 668, 48);
                double p = Game.Probability(g, item);
                Centered(L("%d%% H", Math.Floor(p * 100 + .5)), x, 718, 84, Ui.F16, C.Gold);
                CoinHover(item.Id, x, 664, 84, 76, p);
                AddButton(x, 664, 84, 76, () => A.CoinAction(item), "COIN");
            }

            // next round: the big red button, under the tune-ups
            Ui.Mouse(out float mx, out float my);
            bool over = mx >= 1054 && mx <= 1166 && my >= 586 && my <= 698;
            ImageAt(Ui.UiImages["next_round"], 1054, 586 + (over ? -3 : 0), 112);
            Centered("NEXT ROUND", 1000, 706, 220, Ui.F20, C.White);
            AddButton(1054, 586, 112, 144, () => Game.LeaveShop(g), "NEXT ROUND");
        }
    }
}
