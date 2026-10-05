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
        const float X0 = 430, Step = 200;

        static void VerticalLabel(string word, float x, float y)
        {
            var chars = Lang.Chars(L(word));
            for (int i = 0; i < chars.Count; i++)
                Centered(i == 0 ? Lang.Upper(chars[i]) : chars[i], x, y + i * 30, 38, Ui.F32, C.White);
        }

        static void Price(int amount, float x, float y, float w, bool affordable) =>
            Centered(amount.ToString(), x, y, w, Ui.F32, affordable ? C.Gold : C.Red);

        public static void Draw()
        {
            var g = Ui.Game;
            Box(0, 0, 1620, 800, C.FeltDark);
            Box(46, 36, 1529, 728, ScreenColor);
            Outline(46, 36, 1529, 728, C.Gold);

            // title, gold, menu
            Color(C.White);
            var title = Title("shop");
            Gfx.Draw(title, 89, 44, 110f / title.Height, 110f / title.Height);
            Button("MENU", 1418, 56, 127, 34, C.PanelLight, A.OpenMenu, true, "START");
            ImageAt(Ui.UiImages["gold"], 1278, 100, 44);
            Text(GameText.Num(g.Player.Gold), 1344, 104, Ui.F32, C.Gold);
            RunModifierView.Draw(316,104,32);

            // reroll (coin offers only), level with the coin icons
            int rerollCost = g.RerollCost;
            Box(89, 228, 240, 140, PanelColor);
            Outline(89, 228, 240, 140, C.PanelLight);
            ImageAt(Ui.UiImages["reroll"], 104, 238, 34);
            Text("REROLL", 157, 245, Ui.F20, C.Muted);
            Price(rerollCost, 89, 274, 240, g.Player.Gold >= rerollCost);
            Button("REROLL", 114, 322, 190, 36, C.Orange, () => Game.RerollShop(g), g.Player.Gold >= rerollCost);

            // COIN row
            bool full = g.Coins.Count >= g.Slots;
            VerticalLabel("Coin", 290, 232);
            for (int i = 0; i < 4; i++)
            {
                float x = X0 + i * Step;
                var offer = i < g.ShopOffers.Count ? g.ShopOffers[i] : null;
                string id = offer?.Id;
                if (id != null)
                {
                    int cost = Game.CoinOfferCost(g, i);
                    Price(cost, x, 174, 140, g.Player.Gold >= cost && !full);
                    CoinImage(id, x + 6, 216, 128);
                    CoinHover(id, x, 216, 140, 128, upgrade: i < g.ShopUpgrades.Count ? g.ShopUpgrades[i] : null);
                    if (i < g.ShopUpgrades.Count && g.ShopUpgrades[i] != null) Centered("UPGRADED", x, 340, 139, Ui.F16, C.Gold);
                    int index = i;
                    Button(full ? "FULL" : "BUY", x, 365, 139, 34, C.Blue, () => Game.Buy(g, index), g.Player.Gold >= cost && !full);
                }
                else Centered("SOLD", x, 268, 139, Ui.F32, C.Muted);
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
                    int cost = Game.Price(g, def.Cost);
                    Price(cost, x, 382, 140, g.Player.Gold >= cost && g.Items.Count < Items.Max);
                    ImageAt(Ui.ItemImages[id], x + 6, 426, 128);
                    TextHover(Lang.ItemName(id), Lang.ItemDescription(id), x + 6, 426, 128, 128);
                    int index = i;
                    Button("BUY", x, 562, 139, 34, C.Blue, () => Game.BuyItem(g, index), g.Player.Gold >= cost && g.Items.Count < Items.Max);
                }
                else Centered("SOLD", x, 474, 139, Ui.F32, C.Muted);
            }

            // PRIZE row (relic), column 4 so it lines up with the last coin offer
            float px = X0 + 3 * Step;
            VerticalLabel("Prize", px - 58, 396);
            if (g.ShopRelic != null)
            {
                int relicCost=Game.Price(g,Game.RelicCost);
                Price(relicCost, px, 382, 140, g.Player.Gold >= relicCost);
                ImageAt(Ui.RelicImages[g.ShopRelic], px + 6, 426, 128);
                TextHover(Lang.RelicName(g.ShopRelic), Lang.RelicDescription(g.ShopRelic), px + 6, 426, 128, 128);
                Button("BUY", px, 562, 139, 34, C.Blue, () => Game.BuyRelic(g), g.Player.Gold >= relicCost);
            }
            else Centered("SOLD", px, 474, 139, Ui.F32, C.Muted);

            // Owned chips and prizes retain their art and hover details throughout shopping.
            Box(89, 390, 240, 188, PanelColor); Outline(89, 390, 240, 188, C.PanelLight);
            Text("CHIPS", 104, 400, Ui.F20, C.Gold);
            for (int i = 0; i < g.Items.Count; i++)
            {
                string id = g.Items[i]; float x = 104 + i * 52;
                ImageAt(Ui.ItemImages[id], x, 426, 40); TextHover(Lang.ItemName(id), Lang.ItemDescription(id), x, 426, 40, 40);
            }
            Text("PRIZES", 104, 482, Ui.F20, C.Gold);
            for (int i = 0; i < g.Relics.Count; i++)
            {
                string id = g.Relics[i]; float x = 104 + i * 34;
                ImageAt(Ui.RelicImages[id], x, 510, 30); TextHover(Lang.RelicName(id), Lang.RelicDescription(id), x, 510, 30, 30);
            }
            Box(1266, 238, 278, 220, PanelColor); Outline(1266, 238, 278, 220, C.Line);
            Centered("DECK TOOL", 1266, 250, 278, Ui.F20, C.Gold);
            var selected = g.SelectedUid.HasValue ? Game.GetCoin(g, g.SelectedUid.Value) : null;
            Centered("COIN REMOVAL", 1266, 286, 278, Ui.F20, C.Face);
            Centered(selected != null ? Lang.CoinName(selected.Id) : L("SELECT A COIN"), 1266, 316, 278, Ui.F16, C.Muted);
            Price(8, 1266, 342, 278, g.Player.Gold >= 8);
            Button("REMOVE", 1306, 394, 197, 36, C.Red, () => { if (g.SelectedUid.HasValue) Game.Remove(g, g.SelectedUid.Value); },
                g.Player.Gold >= 8 && g.Coins.Count > 1);

            // your deck
            Text(L("YOUR DECK  %d / %d", g.Coins.Count, g.Slots), 89, 612, Ui.F20, C.Gold);
            Text(full ? "DECK FULL  -  BUY A SLOT OR REMOVE A COIN" : "CLICK A COIN TO SELECT IT", 430, 618, Ui.F16, full ? C.Orange : C.Muted);
            for (int i = 0; i < Game.DeckMax; i++)
            {
                float x = 89 + i * 116;
                var item = i < g.Coins.Count ? g.Coins[i] : null;
                bool chosen = item != null && item.Uid == g.SelectedUid;
                if (i >= g.Slots)
                {
                    bool next=i==g.Slots;Box(x,664,106,76,C.SlotDk);Outline(x,664,106,76,next?C.Gold:C.Line);
                    Padlock(x + 53, next ? 690 : 702);
                    if(next){TextHover("EXTRA SLOT", "Buy one more deck slot. A bigger deck means a bigger quota.", x,664,106,76);int slotCost=Game.SlotPrice(g);Centered("+"+slotCost+" GOLD",x,710,106,Ui.F16,g.Player.Gold>=slotCost?C.Gold:C.Red);AddButton(x,664,106,76,()=>Game.BuySlot(g),"BUY SLOT");}
                    continue;
                }
                Box(x, 664, 106, 76, item != null ? PanelColor : C.Slot);
                Outline(x, 664, 106, 76, chosen ? C.Orange : C.PanelLight);
                if (item == null) continue;
                CoinImage(item.Id, x + 23, 665, 60);
                double p = Game.Probability(g, item);
                Centered(L("%d%% H", Math.Floor(p * 100 + .5)), x, 722, 106, Ui.F16, C.Gold);
                CoinHover(item.Id, x, 664, 106, 76, p, upgrade: item.Upgrade, tieProbability: Game.TieProbability(g,item));
                AddButton(x, 664, 106, 76, () => A.CoinAction(item), "COIN");
            }

            // next round: the big red button, under the tune-ups
            Ui.Mouse(out float mx, out float my);
            bool over = mx >= 1334 && mx <= 1476 && my >= 568 && my <= 710;
            ImageAt(Ui.UiImages["next_round"], 1334, 568 + (over ? -3 : 0), 142);
            Centered("NEXT ROUND", 1266, 714, 278, Ui.F20, C.White);
            AddButton(1334, 568, 142, 166, () => Game.LeaveShop(g), "NEXT ROUND");
        }
    }
}
