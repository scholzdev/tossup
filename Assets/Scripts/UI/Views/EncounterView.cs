using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    public static class EncounterView
    {
        static GameState G => Ui.Game;
        static double Pct(double p) => Math.Floor(p * 100 + .5);

        // The coins shown in the left panel: the next Visible coins of the bank (or the opening hand).
        static List<CoinInst> BankCoins()
        {
            var uids = G.Mulligan != null ? G.Mulligan.Hand : G.Encounter.Queue;
            var list = new List<CoinInst>();
            for (int i = 0; i < Math.Min(Game.Visible, uids.Count); i++) list.Add(Game.GetCoin(G, uids[i]));
            return list;
        }

        // Opening hand: the first coin that would play sits on the stage as always; the hand hovers over it as
        // a row of floating cards. Click to mark, Discard throws the marked ones away (free), then start.
        static int FirstUnmarked()
        {
            foreach (int uid in G.Mulligan.Hand)
                if (!Ui.Marked.Contains(uid)) return uid;
            return G.Mulligan.Hand[0];
        }

        static void Tab(float x, float y, Rgba fill, string label)
        {
            Color(fill);
            Gfx.Rectangle(true, x, y, 76, 18, 4);
            Centered(label, x, y, 76, Ui.F16, C.Ink);
        }

        static void DrawMulligan()
        {
            var hand = G.Mulligan.Hand;
            float w = 170, h = 220, gap = 16;
            float x0 = 640 - (hand.Count * w + (hand.Count - 1) * gap) / 2;
            double now = Ui.Platform.Time;
            Color(C.Ink, .78f); // dim everything behind the hand
            Gfx.Rectangle(true, 0, 0, 1280, 800);
            Centered("OPENING HAND", 0, 214, 1280, Ui.F32, C.Gold);
            Centered("DISCARD COINS YOU DO NOT WANT  -  FREE  -  THEY STAY OUT FOR THE LEVEL", 0, 254, 1280, Ui.F16, C.Muted);
            int first = FirstUnmarked();
            for (int i = 0; i < hand.Count; i++)
            {
                int uid = hand[i];
                var owned = Game.GetCoin(G, uid);
                var def = Content.Coins[owned.Id];
                bool marked = Ui.Marked.Contains(uid);
                float x = x0 + i * (w + gap);
                float y = 300 + (float)Math.Floor(Math.Sin(now * 2 + i + 1) * 3 + .5) - (marked ? 14 : 0);
                Color(C.Black, .4f);
                Gfx.Rectangle(true, x + 4, y + 10, w, h, 6); // shadow: the cards float
                Box(x, y, w, h, marked ? C.Marked : C.Card);
                Outline(x, y, w, h, marked ? C.Red : uid == first ? C.Gold : C.Line);
                CoinImage(owned.Id, x + (w - 80) / 2, y + 12, 80);
                Centered(Lang.Upper(Lang.CoinName(owned.Id)), x, y + 102, w, Ui.F20, C.Face);
                Centered(L("%d%% HEADS", Pct(Game.Probability(G, owned))), x, y + 128, w, Ui.F16, C.Gold);
                Text(L("H") + " " + Effects(def.Heads), x + 12, y + 156, Ui.F16, C.Blue);
                Text(L("T") + " " + Effects(def.Tails), x + 12, y + 180, Ui.F16, C.Red);
                if (marked) Tab(x + (w - 76) / 2, y - 9, C.Red, "DISCARD");
                else if (uid == first) Tab(x + (w - 76) / 2, y - 9, C.Gold, "PLAYS FIRST");
                if (def.EnergyCost > 0) Text("E" + def.EnergyCost, x + w - 30, y + 10, Ui.F16, C.Orange);
                CoinHover(owned.Id, x, y, w, h, Game.Probability(G, owned));
                AddButton(x, y, w, h, () => A.ToggleMark(uid), "CARD");
            }
            Centered("CLICK COINS TO MARK THEM, THEN PRESS DISCARD", 0, 560, 1280, Ui.F16, C.Muted);
            int markedCount = A.MarkedCount();
            IconButton(markedCount > 0 ? L("DISCARD %d", markedCount) : "DISCARD", Ui.UiImages["discard"], 330, 676, 200, 64,
                C.Red, A.DiscardMarked, markedCount > 0 && markedCount < hand.Count);
            IconButton("START LEVEL", Ui.UiImages["start_level"], 550, 676, 260, 64, C.Green, A.NextOrFlip);
        }

        static void DrawFlipAnimation()
        {
            var anim = Ui.FlipAnimation;
            if (anim == null) return;
            double progress = Math.Min(1, anim.Elapsed / anim.Duration);
            // quartic ease-out so the spin slows to a tense stop; even half-turn = Tails up, odd = Heads up
            int turns = anim.Outcome == Side.Heads ? 9 : 10;
            double phase = turns * (1 - Math.Pow(1 - progress, 4));
            float squash = (float)Math.Max(.06, Math.Abs(Math.Cos(phase * Math.PI)));
            bool heads = (long)Math.Floor(phase + .5) % 2 == 1; // the side changes at the thin edge-on moments
            float lift = (float)(45 * Math.Sin(Math.Min(1, progress / .75) * Math.PI));
            string word = anim.Outcome == Side.Tie ? "EDGE" : L(heads ? "HEADS" : "TAILS");
            Gfx.Push();
            Gfx.Translate(770, 385 - lift);
            Gfx.Scale(squash, 1);
            Color(C.White);
            var image = Ui.CoinImages[anim.Id];
            Gfx.Draw(image, -150, -150, 300f / image.Width, 300f / image.Height);
            Color(C.Ink);
            Gfx.Rectangle(true, -90, 76, 180, 44, 6);
            Text(word, -Ui.F32.GetWidth(word) / 2f, 82, Ui.F32, heads ? C.Blue : C.Red);
            Gfx.Pop();
        }

        public static void Draw()
        {
            var g = G;
            var e = g.Encounter;
            Box(0, 0, 1280, 800, C.FeltDark);
            Box(36, 36, 1208, 728, C.Screen);
            Outline(36, 36, 1208, 728, C.Gold);

            // header: logo + level, points in the middle, menu and stats on the right
            Color(C.White);
            var logo = Ui.UiImages["logo"];
            float logoScale = 64f / logo.Height;
            Gfx.Draw(logo, 70, 46, logoScale, logoScale);
            Text(e.Endless != null ? L("LEVEL %d", g.EncounterIndex) : L("LEVEL %d / 8", g.EncounterIndex), 70, 118, Ui.F16, C.Muted);
            Text(e.Endless != null ? L("ENDLESS %d", e.Endless.Value) : e.Boss ? L("THE HOUSE") : Lang.Upper(L(e.Name)), 70, 136, Ui.F20,
                e.Boss ? C.Red : C.Face);
            bool met = e.Quota <= 0;
            string caption = "POINTS";
            var captionColor = C.Muted;
            if (e.Cleared) { caption = "QUOTA MET  -  EXTRA POINTS PAY GOLD"; captionColor = C.Green; }
            else if (e.Boss) { caption = "THE HOUSE  -  EVERY 5TH FLIP IS INVERTED"; captionColor = C.Red; }
            else if (e.Endless != null) { caption = "INVERTED  -  EVERY 5TH FLIP"; captionColor = C.Red; }
            Centered(caption, 330, 48, 580, Ui.F16, captionColor);
            Centered(GameText.Num(e.Scored) + " / " + GameText.Num(e.MaxQuota), 330, 68, 580, Ui.F48, met ? C.Green : C.Gold);
            Color(C.SlotDk);
            Gfx.Rectangle(true, 330, 128, 580, 20, 4);
            Color(met ? C.Green : C.Gold);
            Gfx.Rectangle(true, 330, 128, 580 * (float)Math.Min(1, e.Scored / e.MaxQuota), 20, 4);
            Outline(330, 128, 580, 20, C.Line, 4);
            Button("MENU", 1120, 56, 100, 34, C.PanelLight, A.OpenMenu);
            RunModifierView.Draw(220,106,34);
            if (e.Cleared)
                IconButton("OPEN SHOP", Ui.UiImages["open_shop"], 930, 54, 170, 38, C.Green, A.OpenShop,
                    Ui.FlipAnimation == null && g.Pending == null && g.Mulligan == null);
            int left = Game.CoinsLeft(g);
            var stats = new[]
            {
                ("coins_left", left.ToString(), left <= 2 ? C.Red : C.Face),
                ("gold", GameText.Num(g.Player.Gold), C.Gold),
                ("energy", GameText.Num(g.Player.Energy), C.Blue),
            };
            for (int i = 0; i < stats.Length; i++)
            {
                float x = 950 + i * 90;
                ImageAt(Ui.UiImages[stats[i].Item1], x, 106, 30);
                Text(stats[i].Item2, x + 36, 106, Ui.F32, stats[i].Item3);
            }

            // left: coin bank, three big cards and one quiet line of numbers
            var remaining = BankCoins();
            Box(70, 170, 240, 480, C.PanelDk);
            Outline(70, 170, 240, 480, C.Line);
            Text("COIN BANK", 84, 184, Ui.F20, C.Gold);
            for (int i = 0; i < Game.Visible; i++)
            {
                float x = 83, y = 236 + i * 112;
                var owned = i < remaining.Count ? remaining[i] : null; // flipped and discarded coins drop off the list
                bool current = owned != null && g.Dealt != null && owned.Uid == g.Dealt.Uid;
                bool marked = owned != null && g.Mulligan != null && Ui.Marked.Contains(owned.Uid); // marking only exists in the opening hand
                Box(x, y, 214, 84, marked ? C.Marked : owned != null ? C.Card : C.SlotDk);
                Outline(x, y, 214, 84, marked ? C.Red : current ? C.Gold : owned != null ? C.Line : C.Ink);
                if (owned != null)
                {
                    float tabX = x + 130;
                    if (marked) Tab(tabX, y - 9, C.Red, "DISCARD");
                    if (current && !marked) Tab(tabX, y - 9, C.Gold, "CURRENT"); // tab on the card's top edge
                    CoinImage(owned.Id, x + 8, y + 10, 64);
                    Text(Lang.Upper(Lang.CoinName(owned.Id)), x + 80, y + 16, Ui.F20, C.Face);
                    Text(L("%d%% HEADS", Pct(Game.Probability(g, owned))), x + 80, y + 46, Ui.F16, C.Gold);
                    int cost = Content.Coins[owned.Id].EnergyCost;
                    if (cost > 0) Text("E" + cost, x + 188, y + 60, Ui.F16, C.Orange);
                    int uid=owned.Uid;
                    if(g.Mulligan==null) AddButton(x,y,214,84,()=> { if(e.BankDiscards>0) Game.DiscardBank(g,uid); else Game.Select(g,uid); },"BANK COIN");
                }
                else Centered("EMPTY SLOT", x, y + 34, 214, Ui.F16, C.Muted);
            }

            if(e.Modifier!=null&&Game.Modifiers.TryGetValue(e.Modifier,out var modifier))
                Text(modifier.Name.ToUpper()+": "+modifier.Description,330,151,Ui.F16,C.Orange);
            Text(L("PILE %d   OUT %d   DECK %d/%d", e.Pile.Count, e.Discards, g.Coins.Count, g.Slots), 84, 590, Ui.F16, C.Muted);

            // active buffs ("next N coins ...") so they are never invisible
            for (int i = 0; i < e.Buffs.Count; i++)
            {
                var buff = e.Buffs[i];
                string label;
                if (buff.Kind == "mult") label = L("BUFF x%d  (%d LEFT)", buff.Amount, buff.Left);
                else if (buff.Kind == "odds") label = L("BUFF +%d%% HEADS  (%d LEFT)", Pct(buff.Amount), buff.Left);
                else if (buff.Kind == "swap") label = L("BUFF: NEXT COIN SWAPS SIDES");
                else label = L("BUFF: NEXT COIN LANDS HEADS");
                if (i < 2) Text(label, 84, 612 + i * 18, Ui.F16, C.Orange);
            }

            // centre: the stage. One big coin, its two effects either side, the odds under it.
            const float SX = 770;
            Box(330, 170, 880, 480, C.Card);
            Outline(330, 170, 880, 480, C.Gold);
            FlipState result = null;
            if (Ui.FlipAnimation == null)
                result = g.Pending ?? (Ui.Holding && g.LastResult != null ? g.LastResult : g.Dealt ?? g.LastResult);
            if (g.Mulligan != null) // the stage shows the coin that would play first
            {
                int uid = FirstUnmarked();
                result = new FlipState { Uid = uid, Probability = Game.Probability(g, Game.GetCoin(g, uid)) };
            }
            var item = result != null ? Game.GetCoin(g, result.Uid) : null;
            string outcome = result != null ? result.Final ?? result.Result : null;
            Centered(g.Mulligan != null ? "" : Ui.FlipAnimation != null ? "FLIPPING" : g.Pending != null ? "CURRENT FLIP" :
                g.Dealt != null && !Ui.Holding ? "DEALT COIN" : item != null ? "LAST FLIP" : "NO COIN", 330, 186, 880, Ui.F20, C.Gold);
            if (result != null && result.Altered != null && result.Raw != null && Ui.FlipAnimation == null)
                Centered(L("ROLLED %s  >  %s  (%s)", Lang.Upper(L(result.Raw)), Lang.Upper(L(outcome)), L(result.Altered)),
                    330, 214, 880, Ui.F16, C.Orange);
            Color(C.PanelDk);
            Gfx.Circle(true, SX, 385, 160);
            if (item != null) CoinFace(SX, 385, 135, null, true, item.Id);
            else if (Ui.FlipAnimation == null) CoinImage("back", SX - 150, 235, 300);
            DrawFlipAnimation();

            // combo meter: consecutive identical results multiply points; drawn in the stage's top right corner
            {
                double len = e.ComboLen;
                string side = e.ComboSide;
                double mult = Math.Min(e.ComboCap, 1 + e.ComboStep * (Math.Max(len, 1) - 1));
                var tint = len < 2 ? C.Muted : side == Side.Heads ? C.Blue : C.Red;
                string label = len >= 1 ? L("COMBO  %s x%d", L(side == Side.Heads ? "HEADS" : "TAILS"), len) : L("COMBO");
                var face = Ui.F16;
                Text(label, 1190 - face.GetWidth(L(label)), 184, face, tint);
                string big = GameText.Format("x%.2f", mult);
                Text(big, 1190 - Ui.F32.GetWidth(big), 202, Ui.F32, len < 2 ? C.Muted : C.Gold);
                if (e.Shield > 0)
                {
                    string shield = L("SHIELD %d", e.Shield);
                    Text(shield, 1190 - face.GetWidth(shield), 238, face, C.Green);
                }
            }

            // the two effects
            string coinId = item != null ? item.Id : Ui.FlipAnimation?.Id;
            var coin = coinId != null ? Content.Coins[coinId] : null;
            for (int k = 0; k < 2; k++)
            {
                float cx = k == 0 ? 354 : 986;
                var accent = k == 0 ? C.Blue : C.Red;
                Box(cx, 320, 200, 130, coin != null ? C.PanelDk : C.SlotDk);
                Outline(cx, 320, 200, 130, coin != null ? accent : C.Line);
                Text(k == 0 ? "HEADS" : "TAILS", cx + 14, 332, Ui.F20, accent);
                Gfx.SetFont(Ui.F16);
                Color(coin != null ? C.Face : C.Muted);
                Gfx.Printf(coin != null ? EffectDescription(k == 0 ? coin.Heads : coin.Tails) : "?", cx + 14, 368, 172);
            }

            // result banner on the coin once it has landed
            if (outcome != null && Ui.FlipAnimation == null)
            {
                string note = "";
                var noteColor = C.Muted;
                if (g.Dealt != null && !Ui.Holding) note = "FLIP IT OR DISCARD";
                else if (g.Pending != null) note = "APPLYING...";
                else if (result.Gained > 0)
                {
                    note = L("+%d POINTS", result.Gained.Value);
                    noteColor = C.Green;
                    if (result.Combo != null && result.Combo.Mult > 1.001) note = L("+%d POINTS  (x%.2f)", result.Gained.Value, result.Combo.Mult);
                }
                else if (result.Penalty > 0)
                {
                    note = L("QUOTA +%d", result.Penalty.Value);
                    noteColor = C.Red;
                }
                else if (result.Gained != null) note = "NO POINTS";
                if (!(g.Dealt != null && !Ui.Holding))
                {
                    var accent = outcome == Side.Heads ? C.Blue : outcome == Side.Tie ? C.Gold : C.Red;
                    Box(SX - 130, 456, 260, 62, C.Ink);
                    Outline(SX - 130, 456, 260, 62, accent);
                    Centered(Lang.Upper(outcome), SX - 130, 460, 260, Ui.F32, accent);
                    Centered(note, SX - 130, 496, 260, Ui.F16, noteColor);
                }
            }

            // name, odds bar
            int shownCost = coin != null ? coin.EnergyCost : 0;
            if (g.Mulligan == null)
                Centered(coin != null ? Lang.Upper(Lang.CoinName(coinId)) : Ui.FlipAnimation != null ? "DRAWING..." : "NO COINS LEFT",
                    330, 548, 880, Ui.F32, C.Face);
            if (g.Mulligan != null)
            {
                // the hand floats over this part
            }
            else if (coin != null && result != null)
            {
                double chance = result.Probability;
                Color(C.Blue);
                Gfx.Rectangle(true, SX - 170, 596, 340 * (float)chance, 10);
                Color(C.Gold);
                Gfx.Rectangle(true, SX - 170 + 340 * (float)chance, 596, 340 * (float)result.TieProbability, 10);
                Color(C.Red);
                Gfx.Rectangle(true, SX - 170 + 340 * (float)(chance + result.TieProbability), 596, 340 * (float)(1 - chance - result.TieProbability), 10);
                Text(L("HEADS %d%%", Pct(chance)), SX - 170, 612, Ui.F16, C.Blue);
                string tailsText = L("TAILS %d%%", Math.Floor((1 - chance - result.TieProbability) * 100 + .5));
                Text(tailsText, SX + 170 - Ui.F16.GetWidth(tailsText), 612, Ui.F16, C.Red);
                if (shownCost > 0 && Ui.FlipAnimation == null) Centered(L("ENERGY COST %d", shownCost), SX - 60, 612, 120, Ui.F16, C.Orange);
            }
            else if (Ui.FlipAnimation == null) Centered("ONE COIN AT A TIME", 330, 596, 880, Ui.F16, C.Muted);

            // bottom: hint on the left, buttons, then chips; no bar behind them
            string hint = null;
            if (g.Mulligan != null) hint = "Mark the coins you do not want.";
            if (e.Cleared) hint = "Keep going for gold, or open the shop.";
            if (g.Dealt == null && g.Mulligan == null && g.Pending == null && Ui.FlipAnimation == null && !Ui.Holding)
                hint = !Game.CanExchange(g) ? "No coins left." : null;
            if (g.Dealt != null && !Ui.Holding && !Game.CanFlip(g)) hint = "Too little energy: discard it.";
            if (g.Peek != null)
            {
                var names = new List<string>();
                foreach (int uid in g.Peek) names.Add(Lang.Upper(Lang.CoinName(Game.GetCoin(g, uid).Id)));
                hint = L("NEXT: %s", string.Join(", ", names));
            }
            Ui.Mouse(out float mx, out float my);
            bool usable = Items.CanUse(g) && Ui.FlipAnimation == null && !Ui.Holding;
            for (int slot = 0; slot < Items.Max; slot++)
            {
                float x = 830 + slot * 128;
                if (slot < g.Items.Count)
                {
                    string id = g.Items[slot];
                    bool over = mx >= x && mx <= x + 120 && my >= 676 && my <= 740;
                    float lift = usable && over ? -3 : 0;
                    Box(x, 676 + lift, 120, 64, usable ? C.Card : C.SlotDk);
                    Outline(x, 676 + lift, 120, 64, usable ? C.Orange : C.Line);
                    ImageAt(Ui.ItemImages[id], x + 6, 686 + lift, 44);
                    Text(Lang.ItemShort(id), x + 54, 700 + lift, Ui.F16, usable ? C.Face : C.Muted);
                    int s = slot;
                    if (usable) AddButton(x, 676, 120, 64, () => A.UseItem(s), "ITEM");
                    if (over) hint = Lang.ItemDescription(id);
                }
                else
                {
                    Box(x, 676, 120, 64, C.SlotDk);
                    Outline(x, 676, 120, 64, C.Line);
                    Centered("ITEM", x, 698, 120, Ui.F16, C.Muted);
                }
            }
            if (hint != null)
            {
                Gfx.SetFont(Ui.F16);
                Color(C.Muted);
                Gfx.Printf(L(hint), 70, 690, 240);
            }
            if (g.Dealt != null && Ui.FlipAnimation == null && !Ui.Holding)
                IconButton("DISCARD", Ui.UiImages["discard"], 330, 676, 200, 64, C.Red, A.DiscardCurrent); // only the current coin
            if(g.Dealt!=null&&e.Flips==0&&e.SideBetSide==null&&g.Mulligan==null&&!Ui.Holding&&Ui.FlipAnimation==null)
            {
                var h=Game.SideBetQuote(g,Side.Heads);var t=Game.SideBetQuote(g,Side.Tails);
                if(h!=null)Button("BET H "+h.Stake+"G",340,642,90,28,C.Blue,()=>A.SideBet(Side.Heads),g.Player.Gold>=h.Stake);
                if(t!=null)Button("BET T "+t.Stake+"G",438,642,90,28,C.Red,()=>A.SideBet(Side.Tails),g.Player.Gold>=t.Stake);
            }
            bool emptyStack = g.Dealt == null && g.Mulligan == null && g.Pending == null && Ui.FlipAnimation == null && !Ui.Holding;
            bool outOfCoins = emptyStack && !e.Cleared && Game.CanExchange(g);
            if (outOfCoins)
            {
                // the run is about to end: a notice over the stage with the three ways on
                Color(C.Ink, .72f);
                Gfx.Rectangle(true, 330, 170, 880, 480, 6);
                Box(500, 250, 540, 320, C.PanelDk);
                Outline(500, 250, 540, 320, C.Red);
                Centered("OUT OF COINS", 500, 272, 540, Ui.F32, C.Red);
                Centered(L("%d POINTS SHORT OF THE QUOTA", e.Quota), 500, 316, 540, Ui.F16, C.Muted);
                IconButton(L("BUY MORE COINS  %d GOLD > %d", Game.ExchangeCost(g), Game.ExchangeGain), Ui.UiImages["exchange"],
                    530, 356, 480, 56, C.Green, A.Exchange);
                IconButton("START AGAIN", Ui.UiImages["start_level"], 530, 424, 480, 56, C.Gold, () => A.Start());
                IconButton("BACK TO MENU", Ui.UiImages["give_up"], 530, 492, 480, 56, C.PanelLight, A.OpenMenu);
            }
            else if (emptyStack)
            {
                // cleared with an empty stack: exchange for more gold, or open the shop
                if (Game.CanExchange(g))
                    IconButton(L("PAY %d > %d COINS", Game.ExchangeCost(g), Game.ExchangeGain), Ui.UiImages["exchange"],
                        550, 676, 260, 64, C.Green, A.Exchange);
            }
            else
            {
                string flipLabel = Ui.FlipAnimation != null ? "FLIPPING..." : Ui.Holding ? "NEXT COIN" : "FLIP";
                // (the original also required a dealt coin while holding, which left "NEXT COIN" greyed out after the
                // last coin of the stack: only Space could continue. Here the button does what Space does.)
                bool canAct = (g.Dealt != null || Ui.Holding) && Ui.FlipAnimation == null && (Ui.Holding || g.Pending == null);
                if (canAct && !Ui.Holding && !Game.CanFlip(g))
                {
                    flipLabel = L("NEED %d ENERGY", Game.FlipCost(g, g.Dealt.Uid));
                    canAct = false;
                }
                if(Ui.Holding&&Game.CanBankCombo(g))
                {
                    Button("BANK "+GameText.Num(e.ComboPot)+"G",550,676,125,64,C.Green,A.BankCombo);
                    Button("PUSH",685,676,125,64,C.Blue,A.PushCombo,Game.CanFlip(g));
                }
                else IconButton(flipLabel, Ui.Holding ? Ui.UiImages["next_coin"] : Ui.UiImages["flip"], 550, 676, 260, 64, C.Blue,
                    A.NextOrFlip, canAct);
            }
            if (g.Mulligan != null) DrawMulligan(); // covers the play area and takes over the bottom bar
        }
    }
}
