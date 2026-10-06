using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    public static class EncounterView
    {
        static GameState G => Ui.Game;
        static double Pct(double p) => Math.Floor(p * 100 + .5);
        public static float BankPanelHeight { get; private set; } = 480;

        // The full bank in play order: queued coins followed by the draw pile.
        static List<CoinInst> BankCoins()
        {
            var uids = G.Mulligan != null ? G.Mulligan.Hand : new List<int>(G.Encounter.Queue);
            if (G.Mulligan == null) uids.AddRange(G.Encounter.Pile);
            var list = new List<CoinInst>();
            for (int i = 0; i < Math.Min(Game.DeckMax, uids.Count); i++) list.Add(Game.GetCoin(G, uids[i]));
            return list;
        }

        static CoinType? BuffCoinType(Buff buff)
        {
            if (buff.TargetType.HasValue) return buff.TargetType;
            return Enum.TryParse(buff.Kind, true, out CoinType type) ? type : (CoinType?)null;
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
                var odds = Game.GetOdds(G, owned);
                Centered(L("%d%% HEADS", Pct(odds.Heads)), x, y + 128, w, Ui.F16, C.Gold);
                if (odds.Heads > 0) Text(L("H") + " " + Effects(def.Heads), x + 12, y + 156, Ui.F16, C.Blue);
                if (odds.Tails > 0) Text(L("T") + " " + Effects(def.Tails), x + 12, y + 180, Ui.F16, C.Red);
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
            if (e.Contract != null && Game.Contracts.TryGetValue(e.Contract.Id, out var contract))
            {
                string progress = e.Contract.Id == "quick_clear" ? L("FLIPS %d / 6", e.Flips) :
                    e.Contract.Id == "clean_run" ? L("DISCARDS %d", e.Discards) :
                    e.Contract.Id == "hot_streak" ? L("BEST COMBO %d / 4", (int)e.BestComboLen) :
                    e.Contract.Id == "bank_once" ? L(e.ComboBanked ? "BANKED" : "NOT BANKED") :
                    e.Contract.Id == "amazon_prime" ? L("COINS LEFT %d / 3", Game.CoinsLeft(g)) : null;
                string contractText = e.Contract.Result == "COMPLETE" ? L("CONTRACT COMPLETE") :
                    e.Contract.Result == "MISSED" ? L("CONTRACT MISSED") :
                    progress != null ? L("CONTRACT: %s", L(contract.Name)) + "  " + progress + "  " + L(contract.Drawback) : null;
                if (contractText != null)
                    Centered(contractText, 330, 152, 880, Ui.F16, e.Contract.Result == "COMPLETE" ? C.Green : C.Gold);
            }
            bool met = e.Quota <= 0;
            int bossEvery = (int)Game.Rule(g, "boss_every", 5);
            string caption = "POINTS";
            var captionColor = C.Muted;
            if (e.Cleared) { caption = "QUOTA MET  -  EXTRA POINTS PAY GOLD"; captionColor = C.Green; }
            else if (e.Boss) { caption = L("THE HOUSE  -  EVERY %dTH FLIP IS INVERTED", bossEvery); captionColor = C.Red; }
            else if (e.Endless != null) { caption = L("INVERTED  -  EVERY %dTH FLIP", bossEvery); captionColor = C.Red; }
            Centered(caption, 330, 48, 580, Ui.F16, captionColor);
            Centered(GameText.Num(e.Scored) + " / " + GameText.Num(e.MaxQuota), 330, 68, 580, Ui.F48, met ? C.Green : C.Gold);
            Color(C.SlotDk);
            Gfx.Rectangle(true, 330, 128, 580, 20, 4);
            Color(met ? C.Green : C.Gold);
            Gfx.Rectangle(true, 330, 128, 580 * (float)Math.Min(1, e.Scored / e.MaxQuota), 20, 4);
            Outline(330, 128, 580, 20, C.Line, 4);
            Button("MENU", 1120, 56, 100, 34, C.PanelLight, A.OpenMenu);
            bool levelDone = e.Cleared && g.Dealt == null && g.Mulligan == null && g.Pending == null &&
                Ui.FlipAnimation == null && !Ui.Holding;
            var headsBet = Game.SideBetQuote(g, Side.Heads);
            var tailsBet = Game.SideBetQuote(g, Side.Tails);
            bool betAvailable = headsBet != null && g.Player.Gold >= headsBet.Stake ||
                tailsBet != null && g.Player.Gold >= tailsBet.Stake;
            if (levelDone)
            {
                if (Game.CanExchange(g))
                    IconButton(L("EXCHANGE %dG", Game.ExchangeCost(g)), Ui.UiImages["exchange"], 930, 54, 170, 38, C.PanelLight, A.Exchange);
            }
            else if (e.Cleared)
                IconButton("OPEN SHOP", Ui.UiImages["open_shop"], 930, 54, 170, 38, C.Green, A.OpenShop,
                    Ui.FlipAnimation == null && g.Pending == null && g.Mulligan == null);
            int left = Game.CoinsLeft(g);
            var stats = new[]
            {
                ("coins_left", left.ToString(), left <= 2 ? C.Red : C.Face),
                ("gold", GameText.Num(g.Player.Gold), C.Gold),
                ("energy", GameText.Num(g.Player.Energy), C.Blue),
            };
            float statsX = 1226;
            for (int i = stats.Length - 1; i >= 0; i--)
            {
                statsX -= 36 + Ui.F32.GetWidth(stats[i].Item2);
                ImageAt(Ui.UiImages[stats[i].Item1], statsX, 106, 30);
                Text(stats[i].Item2, statsX + 36, 106, Ui.F32, stats[i].Item3);
                statsX -= 22;
            }

            // left: full bank, with a separate mode for Crystal Ball's free discard
            var remaining = BankCoins();
            bool discardAvailable = e.BankDiscards > 0 && g.Dealt != null && g.Pending == null &&
                Ui.FlipAnimation == null && !Ui.Holding && g.Mulligan == null;
            if (!discardAvailable) Ui.BankDiscardMode = false;
            bool picking = discardAvailable && Ui.BankDiscardMode;
            bool bankSelectable = !picking && (g.Dealt != null || Ui.Holding) && g.Pending == null &&
                Ui.FlipAnimation == null && g.Mulligan == null;
            int rowStart = discardAvailable ? 236 : 226;
            int buffRows = Math.Min(2, e.Buffs.Count) + (e.Buffs.Count > 2 ? 1 : 0);
            float footerHeight = 22 + buffRows * 18;
            int bankRows = Math.Max(1, remaining.Count);
            float rowStep = Math.Min(60, (650 - rowStart - footerHeight) / bankRows);
            float rowHeight = rowStep - 4;
            float footerY = rowStart + bankRows * rowStep + 4;
            BankPanelHeight = rowStart + bankRows * rowStep + footerHeight - 170;
            Box(70, 170, 240, BankPanelHeight, C.PanelDk);
            Outline(70, 170, 240, BankPanelHeight, C.Line);
            Text("COIN BANK", 84, 184, Ui.F20, C.Gold);
            if (discardAvailable)
                Button(picking ? "DISCARD MODE: ON" : "DISCARD MODE: OFF", 84, 205, 214, 24,
                    picking ? C.Orange : C.PanelLight, () => A.ToggleBankDiscardMode());
            else if (bankSelectable) Text("CLICK A COIN TO PLAY IT", 84, 208, Ui.F16, C.Muted);
            if (remaining.Count == 0)
                Centered("NO COINS LEFT", 83, rowStart + (rowHeight - Ui.F20.Height) / 2, 214, Ui.F20, C.Muted);
            for (int i = 0; i < remaining.Count; i++)
            {
                float x = 83, y = rowStart + i * rowStep;
                var owned = remaining[i]; // flipped and discarded coins drop off the list
                bool current = g.Dealt != null && owned.Uid == g.Dealt.Uid;
                bool marked = g.Mulligan != null && Ui.Marked.Contains(owned.Uid); // marking only exists in the opening hand
                bool discardable = i < Game.Visible;
                Box(x, y, 214, rowHeight, marked ? C.Marked : owned != null ? C.Card : C.SlotDk);
                Outline(x, y, 214, rowHeight, picking && discardable ? C.Orange : marked ? C.Red : current ? C.Gold : owned != null ? C.Line : C.Ink);
                CoinType? targetType = null;
                foreach (var buff in e.Buffs)
                {
                    var buffType = BuffCoinType(buff);
                    if (buff.Fresh || !buffType.HasValue || !Game.HasType(owned, buffType.Value)) continue;
                    targetType = buffType;
                    break;
                }
                if (targetType.HasValue)
                {
                    Color(CoinTypeColor(targetType.Value));
                    Gfx.Rectangle(true, x + 209, y + 3, 4, rowHeight - 6);
                }
                float iconSize = Math.Min(48, rowHeight - 4);
                CoinImage(owned.Id, x + 4, y + (rowHeight - iconSize) / 2, iconSize);
                float nameX = x + iconSize + 10;
                Gfx.SetScissor(nameX, y, x + 116 - nameX, rowHeight);
                Text(Lang.Upper(Lang.CoinName(owned.Id)), nameX, y + (rowHeight - Ui.F20.Height) / 2, Ui.F20,
                    picking && !discardable ? C.Muted : current ? C.Gold : C.Face);
                Gfx.ClearScissor();
                CoinHover(owned.Id, x, y, 214, rowHeight, Game.Probability(g, owned), upgrade: owned.Upgrade,
                    tieProbability: Game.TieProbability(g, owned));
                Text(L("%d%% H", Pct(Game.Probability(g, owned))), x + 120, y + (rowHeight - Ui.F16.Height) / 2, Ui.F16, C.Gold);
                int cost = Content.Coins[owned.Id].EnergyCost;
                if (cost > 0) Text("E" + cost, x + 181, y + (rowHeight - Ui.F16.Height) / 2, Ui.F16, C.Orange);
                int uid=owned.Uid;
                if (picking && discardable) AddButton(x,y,214,rowHeight,()=>A.DiscardBank(uid),"BANK DISCARD");
                else if (bankSelectable && !current) AddButton(x,y,214,rowHeight,()=>A.CoinAction(owned),"BANK COIN");
            }

            if (e.Modifier != null && Game.Modifiers.TryGetValue(e.Modifier, out var modifier))
            {
                Text("MODIFIER", 346, 556, Ui.F16, C.Muted);
                Text(Lang.Upper(L(modifier.Name)), 346, 576, Ui.F20, C.Orange);
                Gfx.SetFont(Ui.F16);
                Color(C.Muted);
                Gfx.Printf(L(modifier.Description), 346, 602, 250);
            }
            Text(L("BANK %d   OUT %d   DECK %d/%d", Game.CoinsLeft(g), e.Discards, g.Coins.Count, g.Slots), 84, footerY, Ui.F16, C.Muted);

            // active buffs ("next N coins ...") so they are never invisible
            for (int i = 0; i < e.Buffs.Count; i++)
            {
                var buff = e.Buffs[i];
                string label;
                if (buff.Kind == "mult") label = L("BUFF x%d  (%d LEFT)", buff.Amount, buff.Left);
                else if (buff.Kind == "odds") label = L("BUFF +%d%% HEADS  (%d LEFT)", Pct(buff.Amount), buff.Left);
                else if (buff.Kind == "swap") label = L("BUFF: NEXT COIN SWAPS SIDES");
                else if (buff.Kind == "heads") label = L("BUFF: NEXT COIN LANDS HEADS");
                else if (buff.Kind == "effect" && buff.AppliedEffect != null)
                {
                    string target = buff.TargetType.HasValue ? L("NEXT %s", L(buff.TargetType.Value.ToString().ToUpperInvariant())) : L("NEXT COIN");
                    string effect = EffectDescription(new[] { buff.AppliedEffect });
                    label = buff.AppliesOn.HasValue
                        ? L("BUFF %s ON %s: %s (%d LEFT)", target, L(buff.AppliesOn.Value.ToString().ToUpperInvariant()), effect, buff.Left)
                        : L("BUFF %s: %s (%d LEFT)", target, effect, buff.Left);
                }
                else label = L("BUFF %s  (%d LEFT)", L(buff.Kind.ToUpper()), buff.Left);
                CoinType? targetType = BuffCoinType(buff);
                if (i < 2) Text(label, 84, footerY + 18 + i * 18, Ui.F16, targetType.HasValue ? CoinTypeColor(targetType.Value) : C.Orange);
            }
            if (e.Buffs.Count > 2) Text(L("+%d MORE BUFFS", e.Buffs.Count - 2), 84, footerY + 18 + 2 * 18, Ui.F16, C.Orange);

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
            string coinId = item?.Id ?? result?.CoinId ?? Ui.FlipAnimation?.Id;
            var oddsCoin = item ?? (g.Pending != null ? Game.GetCoin(g, g.Pending.Uid) : null);
            Odds displayedOdds = result != null
                ? new Odds(result.Probability, result.TieProbability, Math.Max(0, 1 - result.Probability - result.TieProbability))
                : g.Pending != null
                    ? new Odds(g.Pending.Probability, g.Pending.TieProbability, Math.Max(0, 1 - g.Pending.Probability - g.Pending.TieProbability))
                    : oddsCoin != null ? Game.GetOdds(g, oddsCoin) : null;
            string outcome = result != null ? result.Final ?? result.Result : null;
            Centered(g.Mulligan != null ? "" : Ui.FlipAnimation != null ? "FLIPPING" : g.Pending != null ? "CURRENT FLIP" :
                g.Dealt != null && !Ui.Holding ? "SELECTED COIN" : item != null ? "LAST FLIP" : "NO COIN", 330, 186, 880, Ui.F20, C.Gold);
            if (result != null && result.Altered != null && result.Raw != null && Ui.FlipAnimation == null)
                Centered(L("ROLLED %s  >  %s  (%s)", Lang.Upper(L(result.Raw)), Lang.Upper(L(outcome)), L(result.Altered)),
                    330, 214, 880, Ui.F16, C.Orange);
            Color(C.PanelDk);
            Gfx.Circle(true, SX, 385, 160);
            if (item != null) CoinFace(SX, 385, 135, null, true, coinId);
            else if (Ui.FlipAnimation == null && coinId != null) CoinFace(SX, 385, 135, null, false, coinId);
            if (Ui.FlipAnimation == null && g.Pending == null && (Ui.Holding || Game.CanFlip(g)))
                AddButton(SX - 150, 235, 300, 300, A.NextOrFlip, "CENTRAL COIN");
            else if (Ui.FlipAnimation == null && coinId == null) CoinImage("back", SX - 150, 235, 300);
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
                if (e.ComboPot > 0)
                {
                    if (Ui.Holding && Game.CanBankCombo(g))
                        Button(L("BANK %dG", e.ComboPot), 1040, 232, 150, 34, C.Green, A.BankCombo);
                    else
                    {
                        string pot = L("POT %dG", e.ComboPot);
                        Text(pot, 1190 - face.GetWidth(pot), 238, face, C.Gold);
                    }
                    Text(L("BREAK LOSES POT"), 1000, 304, Ui.F16, C.Red);
                }
                if (e.SideBetSide != null)
                {
                    string bet = e.SideBetOutcome == "WON" ? L("BET WON  +%dG", e.SideBetPayout) :
                        e.SideBetOutcome == "LOST" ? L("BET LOST  -%dG", e.SideBetCost) :
                        e.SideBetOutcome == "PUSH" ? L("BET PUSHED") :
                        L("BET %s  %dG", L(e.SideBetSide.ToUpper()), e.SideBetCost);
                    var betColor = e.SideBetOutcome == "WON" ? C.Green : e.SideBetOutcome == "LOST" ? C.Red : C.Gold;
                    Text(bet, 940, 270, face, betColor);
                }
            }

            // the two effects
            var coin = coinId != null ? Content.Coins[coinId] : null;
            for (int k = 0; k < 2; k++)
            {
                if (displayedOdds != null && (k == 0 ? displayedOdds.Heads : displayedOdds.Tails) <= 0) continue;
                float cx = k == 0 ? 354 : 986;
                var accent = k == 0 ? C.Blue : C.Red;
                Box(cx, 320, 200, 130, coin != null ? C.PanelDk : C.SlotDk);
                Outline(cx, 320, 200, 130, coin != null ? accent : C.Line);
                Text(k == 0 ? "HEADS" : "TAILS", cx + 14, 332, Ui.F20, accent);
                Gfx.SetFont(Ui.F16);
                Color(coin != null ? C.Face : C.Muted);
                Gfx.Printf(coin != null ? CoinOutcomeDescription(coin, k == 0 ? Side.Heads : Side.Tails) : "?", cx + 14, 368, 172);
            }

            // result banner on the coin once it has landed
            if (outcome != null && Ui.FlipAnimation == null)
            {
                string note = "";
                var noteColor = C.Muted;
                if (g.Dealt != null && !Ui.Holding) note = "FLIP OR SELECT ANOTHER COIN";
                else if (g.Pending != null) note = "APPLYING...";
                else if (result.Gained > 0 && result.Penalty > 0)
                {
                    note = L("+%d PTS / QUOTA +%d", result.Gained.Value, result.Penalty.Value);
                    noteColor = C.Purple;
                }
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
                    var accent = outcome == Side.Heads ? C.Blue : outcome == Side.Tie ? C.Purple : C.Red;
                    string label = outcome == Side.Tie ? "EDGE" : Lang.Upper(outcome);
                    float bannerWidth = Math.Max(152, Math.Max(Ui.F32.GetWidth(label), Ui.F16.GetWidth(note)) + 32);
                    float bannerX = SX - bannerWidth / 2;
                    Box(bannerX, 456, bannerWidth, 62, C.Ink);
                    Outline(bannerX, 456, bannerWidth, 62, accent);
                    Centered(label, bannerX, 460, bannerWidth, Ui.F32, accent);
                    Centered(note, bannerX, 496, bannerWidth, Ui.F16, noteColor);
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
            else if (coin != null && result != null && displayedOdds != null)
            {
                double chance = displayedOdds.Heads;
                double edgeChance = displayedOdds.Edge;
                double tailsChance = displayedOdds.Tails;
                Color(C.Blue);
                Gfx.Rectangle(true, SX - 170, 596, 340 * (float)chance, 10);
                Color(C.Purple);
                Gfx.Rectangle(true, SX - 170 + 340 * (float)chance, 596, 340 * (float)edgeChance, 10);
                Color(C.Red);
                Gfx.Rectangle(true, SX - 170 + 340 * (float)(chance + edgeChance), 596, 340 * (float)tailsChance, 10);
                if (chance > 0) Text(L("HEADS %d%%", Pct(chance)), SX - 170, 612, Ui.F16, C.Blue);
                if (edgeChance > 0) Centered(L("EDGE %d%%", Pct(edgeChance)), SX - 55, 612, 110, Ui.F16, C.Purple);
                string tailsText = L("TAILS %d%%", Math.Floor(tailsChance * 100 + .5));
                if (tailsChance > 0) Text(tailsText, SX + 170 - Ui.F16.GetWidth(tailsText), 612, Ui.F16, C.Red);
                if (shownCost > 0 && Ui.FlipAnimation == null && !(e.Flips == 0 && e.SideBetSide == null && betAvailable))
                    Centered(L("ENERGY COST %d", shownCost), SX - 60, edgeChance > 0 ? 632 : 612, 120, Ui.F16, C.Orange);
            }
            else if (Ui.FlipAnimation == null) Centered("ONE COIN AT A TIME", 330, 596, 880, Ui.F16, C.Muted);

            // bottom: hint on the left, buttons, then chips; no bar behind them
            string hint = null;
            if (g.Mulligan != null) hint = "Mark the coins you do not want.";
            if (e.Cleared) hint = "Keep going for gold, or open the shop.";
            if (g.Dealt == null && g.Mulligan == null && g.Pending == null && Ui.FlipAnimation == null && !Ui.Holding)
                hint = !Game.CanExchange(g) ? "No coins left." : null;
            if (g.Dealt != null && !Ui.Holding && Game.FlipCost(g, g.Dealt.Uid) > g.Player.Energy && Game.CoinsLeft(g) <= 1)
                hint = L("LAST COIN EMERGENCY FEE: %dG", Math.Min(2, g.Player.Gold));
            else if (g.Dealt != null && !Ui.Holding && !Game.CanFlip(g)) hint = L("TOO LITTLE ENERGY: SELECT ANOTHER COIN");
            if (g.Peek != null)
            {
                var names = new List<string>();
                foreach (int uid in g.Peek) names.Add(Lang.Upper(Lang.CoinName(Game.GetCoin(g, uid).Id)));
                hint = L("NEXT: %s", string.Join(", ", names));
            }
            Ui.Mouse(out float mx, out float my);
            bool usable = Items.CanUse(g) && Ui.FlipAnimation == null && !Ui.Holding;
            Centered(L("CHIPS"), 330, 654, 280, Ui.F16, C.Gold);
            var heldItems = new List<(int slot, string id)>();
            for (int slot = 0; slot < Math.Min(Items.Max, g.Items.Count); slot++)
                if (g.Items[slot] != null) heldItems.Add((slot, g.Items[slot]));
            if (heldItems.Count > 0)
            {
                float chipWidth = heldItems.Count * 56 + (heldItems.Count - 1) * 12;
                float chipsX = 330 + (280 - chipWidth) / 2;
                for (int i = 0; i < heldItems.Count; i++)
                {
                    var chip = heldItems[i];
                    float x = chipsX + i * 68;
                    float y = 698;
                    string id = chip.id;
                    bool over = mx >= x && mx <= x + 56 && my >= y && my <= y + 56;
                    float lift = usable && over ? -3 : 0;
                    Box(x, y + lift, 56, 56, usable ? C.Card : C.SlotDk);
                    Outline(x, y + lift, 56, 56, usable ? C.Orange : C.Line);
                    ImageAt(Ui.ItemImages[id], x + 12, y + 3 + lift, 32);
                    Centered(Lang.ItemShort(id), x, y + 37 + lift, 56, Ui.F16, usable ? C.Face : C.Muted);
                    int s = chip.slot;
                    if (usable) AddButton(x, y, 56, 56, () => A.UseItem(s), "ITEM");
                    if (over) hint = Lang.ItemDescription(id);
                }
            }
            int prizeCount = g.Augments.Count + (g.RunEncounterId != null ? 1 : 0);
            const float prizeX = 930, prizeWidth = 280, prizeSize = 44, prizeGap = 6;
            Centered(L("PRIZES"), prizeX, 654, prizeWidth, Ui.F16, C.Gold);
            if (prizeCount > 0)
            {
                float contentsWidth = prizeCount * prizeSize + (prizeCount - 1) * prizeGap;
                RunModifierView.Draw(prizeX + (prizeWidth - contentsWidth) / 2, 680, prizeSize);
            }
            if (hint != null)
            {
                Gfx.SetFont(Ui.F16);
                Color(C.Muted);
                Gfx.Printf(L(hint), 70, 690, 240);
            }
            bool showSideBets = g.Dealt != null && g.Mulligan == null && g.Pending == null && Ui.FlipAnimation == null &&
                !Ui.Holding && e.Flips == 0 && e.SideBetSide == null && (headsBet != null || tailsBet != null);
            if (showSideBets)
            {
                const float betX = 570;
                if (headsBet != null)
                    Button(L("BET %s %dG  >  %dG", L("HEADS"), headsBet.Stake, headsBet.Payout), betX, 652, 190, 34,
                        C.Blue, () => A.SideBet(Side.Heads), g.Player.Gold >= headsBet.Stake);
                if (tailsBet != null)
                    Button(L("BET %s %dG  >  %dG", L("TAILS"), tailsBet.Stake, tailsBet.Payout), betX + 210, 652, 190, 34,
                        C.Red, () => A.SideBet(Side.Tails), g.Player.Gold >= tailsBet.Stake);
            }
            bool emptyStack = g.Dealt == null && g.Mulligan == null && g.Pending == null && Ui.FlipAnimation == null && !Ui.Holding;
            bool outOfCoins = emptyStack && !e.Cleared && Game.CanExchange(g);
            if (outOfCoins)
            {
                // the run is about to end: a notice over the stage with the three ways on
                Color(C.Ink, .72f);
                Gfx.Rectangle(true, 330, 170, 880, 480, 6);
                Box(500, 230, 540, 380, C.PanelDk);
                Outline(500, 230, 540, 380, C.Red);
                Centered("OUT OF COINS", 500, 246, 540, Ui.F32, C.Red);
                Centered(L("%d POINTS SHORT OF THE QUOTA", e.Quota) + "  -  " + L("EXCHANGES LEFT: %d", Game.ExchangesLeft(g)),
                    500, 290, 540, Ui.F16, C.Muted);
                int buttonOffset = 0;
                if (Game.CanBankCombo(g))
                {
                    Button(L("BANK %dG", e.ComboPot), 530, 326, 480, 48, C.Green, A.BankCombo);
                    buttonOffset = 58;
                }
                IconButton(L("BUY MORE COINS  %d GOLD > %d", Game.ExchangeCost(g), Game.ExchangeGain), Ui.UiImages["exchange"],
                    530, 356 + buttonOffset, 480, 56, C.Green, A.Exchange);
                IconButton("START AGAIN", Ui.UiImages["start_level"], 530, 424 + buttonOffset, 480, 56, C.Gold, () => A.Start());
                IconButton("BACK TO MENU", Ui.UiImages["give_up"], 530, 492 + buttonOffset, 480, 56, C.PanelLight, A.OpenMenu);
            }
            else if (emptyStack)
            {
                // Once the quota is met, the shop is the main action and an exchange remains in the header.
                if (e.Cleared)
                    IconButton("OPEN SHOP", Ui.UiImages["open_shop"], 640, 698, 260, 56, C.Green, A.OpenShop);
            }
            else if (Ui.Holding && g.Dealt != null && Game.CanBankCombo(g))
            {
                bool canPush = Game.CanFlip(g);
                string label = canPush ? "PUSH" : L("NEED %d ENERGY", Game.FlipCost(g, g.Dealt.Uid));
                IconButton(label, Ui.UiImages["flip"], 640, 698, 260, 56, C.Blue, A.PushCombo, canPush);
            }
            else
            {
                string flipLabel = Ui.FlipAnimation != null ? "FLIPPING..." : Ui.Holding ? g.Dealt != null ? "NEXT COIN" : "CONTINUE" : "FLIP";
                bool canAct = Ui.FlipAnimation == null && g.Pending == null && (Ui.Holding || g.Dealt != null);
                if (canAct && !Ui.Holding && !Game.CanFlip(g))
                {
                    flipLabel = L("NEED %d ENERGY", Game.FlipCost(g, g.Dealt.Uid));
                    canAct = false;
                }
                IconButton(flipLabel, Ui.Holding ? Ui.UiImages["next_coin"] : Ui.UiImages["flip"], 640, 698, 260, 56, C.Blue,
                    A.NextOrFlip, canAct);
            }
            if (g.Mulligan != null) DrawMulligan(); // covers the play area and takes over the bottom bar
        }
    }
}
