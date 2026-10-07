using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    // Shop (full screen): coins, chips (items) and prizes (relics), a reroll for the coin offers, and
    // tune-ups for the coin you select in the deck strip. Buying a locked coin unlocks it for good.
    public static class ShopView
    {
        static readonly Rgba PanelColor = new Rgba(.06f, .20f, .23f);

        // Grid: offer columns start at X0 with a fixed step, so every row lines up with the coin row.
        const float X0 = 340, Step = 150;

        static void RowLabel(string word, float x, float y) =>
            Text(Lang.Upper(L(word)), x, y, Ui.F16, C.Gold);

        static void Price(int amount, float x, float y, float w, bool affordable) =>
            Centered(amount.ToString(), x, y, w, Ui.F32, affordable ? C.Gold : C.Red);

        public static void Draw()
        {
            var g = Ui.Game;
            Frame(null);

            // title, gold, menu
            Color(C.White);
            var title = Title("shop");
            Gfx.Draw(title, 70, 44, 110f / title.Height, 110f / title.Height);
            Button("MENU", 1120, 56, 100, 34, C.PanelLight, A.OpenMenu);
            ImageAt(Ui.UiImages["gold"], 1010, 100, 44);
            Text(GameText.Num(g.Player.Gold), 1062, 104, Ui.F32, C.Gold);
            RunModifierView.Draw(460, 104, 32);

            // reroll (coin offers only), level with the coin icons
            int rerollCost = g.RerollCost;
            Box(70, 228, 190, 140, PanelColor);
            Outline(70, 228, 190, 140, C.PanelLight);
            ImageAt(Ui.UiImages["reroll"], 82, 238, 34);
            Text("REROLL", 124, 245, Ui.F20, C.Muted);
            Price(rerollCost, 70, 274, 190, g.Player.Gold >= rerollCost);
            Button("REROLL", 90, 322, 150, 36, C.Orange, () => Game.RerollShop(g), g.Player.Gold >= rerollCost);

            // COIN row
            bool full = g.Coins.Count >= g.Slots;
            RowLabel("Coin", 270, 240);
            for (int i = 0; i < 4; i++)
            {
                float x = X0 + i * Step;
                string id = i < g.ShopOffers.Count ? g.ShopOffers[i]?.Id : null;
                if (id != null)
                {
                    int cost = Game.CoinOfferCost(g, i);
                    Price(cost, x, 186, 110, g.Player.Gold >= cost && !full);
                    CoinImage(id, x + 7, 228, 96);
                    CoinHover(id, x, 228, 110, 96);
                    int index = i;
                    Button(full ? "FULL" : "BUY", x, 336, 110, 34, C.Blue, () => Game.Buy(g, index), g.Player.Gold >= cost && !full);
                }
                else Centered("SOLD", x, 268, 110, Ui.F32, C.Muted);
            }

            // CHIP row (items), columns 1-2
            RowLabel("Chip", 270, 442);
            for (int i = 0; i < 2; i++)
            {
                float x = X0 + i * Step;
                string id = i < g.ShopItems.Count ? g.ShopItems[i] : null;
                if (id != null)
                {
                    var def = Content.Items[id];
                    int cost = Game.Price(g, def.Cost);
                    Price(cost, x, 392, 110, g.Player.Gold >= cost && g.Items.Count < Items.Max);
                    ImageAt(Ui.ItemImages[id], x + 7, 434, 96);
                    TextHover(Lang.ItemName(id), Lang.ItemDescription(id), x + 7, 434, 96, 96);
                    int index = i;
                    Button("BUY", x, 542, 110, 34, C.Blue, () => Game.BuyItem(g, index), g.Player.Gold >= cost && g.Items.Count < Items.Max);
                }
                else Centered("SOLD", x, 474, 110, Ui.F32, C.Muted);
            }

            // PRIZE row (relic), column 4 so it lines up with the last coin offer
            float px = X0 + 3 * Step;
            RowLabel("Prize", 710, 442);
            if (g.ShopRelic != null)
            {
                int relicCost=Game.Price(g,Game.RelicCost);
                Price(relicCost, px, 392, 110, g.Player.Gold >= relicCost);
                ImageAt(Ui.RelicImages[g.ShopRelic], px + 7, 434, 96);
                TextHover(Lang.RelicName(g.ShopRelic), Lang.RelicDescription(g.ShopRelic), px + 7, 434, 96, 96);
                Button("BUY", px, 542, 110, 34, C.Blue, () => Game.BuyRelic(g), g.Player.Gold >= relicCost);
            }
            else Centered("SOLD", px, 474, 110, Ui.F32, C.Muted);

            // Services for the selected coin
            Box(1000, 190, 220, 370, PanelColor);
            Outline(1000, 190, 220, 370, C.Line);
            Centered("COIN SERVICES", 1000, 200, 220, Ui.F20, C.Gold);
            var selected = g.SelectedUid.HasValue ? Game.GetCoin(g, g.SelectedUid.Value) : null;
            Centered(selected == null ? "SELECT A COIN" : Lang.Upper(selected.Definition.Name), 1000, 225, 220, Ui.F16, C.Muted);
            float tuneY = 248;
            Box(1008, tuneY, 204, 64, C.Card);
            Centered("COIN REMOVAL", 1008, tuneY + 6, 204, Ui.F16, C.Face);
            TextHover("Coin Removal", "Drop the selected coin", 1008, tuneY, 204, 34);
            Text("8 G", 1018, tuneY + 39, Ui.F16, g.Player.Gold >= 8 ? C.Gold : C.Red);
            Button("REMOVE", 1102, tuneY + 34, 100, 26, C.Red, () => { if (g.SelectedUid.HasValue) Game.Remove(g, g.SelectedUid.Value); },
                g.Player.Gold >= 8 && selected != null && g.Coins.Count > 1, face: Ui.F16);

            // your deck
            Text(L("YOUR DECK  %d / %d", g.Coins.Count, g.Slots), 70, 612, Ui.F20, C.Gold);
            Text(full ? "DECK FULL  -  BUY A SLOT OR REMOVE A COIN" : "CLICK A COIN TO SELECT IT", 340, 618, Ui.F16, full ? C.Orange : C.Muted);
            var held = new List<string>();
            foreach (var id in g.Items) held.Add(Lang.ItemShort(id));
            foreach (var id in g.Relics) held.Add(Lang.Upper(Lang.RelicName(id)));
            if (held.Count > 0) Text(L("HELD  %s", string.Join(", ", held)), 340, 638, Ui.F16, C.Orange);
            for (int i = 0; i < Game.DeckMax; i++)
            {
                float x = 70 + i * 92;
                var item = i < g.Coins.Count ? g.Coins[i] : null;
                bool chosen = item != null && item.Uid == g.SelectedUid;
                if (i >= g.Slots)
                {
                    bool next=i==g.Slots;Box(x,664,84,76,C.SlotDk);Outline(x,664,84,76,next?C.Gold:C.Line);
                    if(next){int slotCost=Game.SlotPrice(g);Centered("+"+slotCost+" GOLD",x,710,84,Ui.F16,g.Player.Gold>=slotCost?C.Gold:C.Red);AddButton(x,664,84,76,()=>Game.BuySlot(g),"BUY SLOT");}
                    continue;
                }
                Box(x, 664, 84, 76, item != null ? PanelColor : C.Slot);
                Outline(x, 664, 84, 76, chosen ? C.Orange : C.PanelLight);
                if (item == null) continue;
                CoinImage(item, x + 18, 668, 48);
                double p = Game.Probability(g, item);
                Centered(L("%d%% H", Math.Floor(p * 100 + .5)), x, 718, 84, Ui.F16, C.Gold);
                CoinHover(item.Id, x, 664, 84, 76, p);
                AddButton(x, 664, 84, 76, () => A.CoinAction(item), "COIN");
            }

            // A large cabinet button closes the shop and starts the next round.
            Button("NEXT ROUND", 1000, 602, 220, 88, C.Red, () => Game.LeaveShop(g));
        }
    }
}
