using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    // A fight as a duel table, top to bottom: the bar, the enemy and its coins, the scoreboard, and your area
    // (what you flipped this round, your hand, and the footer with chips, prizes and END ROUND).
    // Click a hand card to flip it in place, then END ROUND.
    public static class EncounterView
    {
        static GameState G => Ui.Game;
        static double Pct(double p) => Math.Floor(p * 100 + .5);

        // The grid every block shares (also read by the tutorial's spotlights).
        public const float Left = 64, Width = 1152, Right = Left + Width, Pad = 14;
        public const float BarY = 48, BarH = 32;
        public const float EnemyY = 88, EnemyH = 110;
        public const float BoardY = 206, BoardH = 190;
        public const float YourY = 404, YourH = 344;
        public const float StripY = YourY + 12, StatusY = YourY + 36, CardsY = YourY + 68, CardsH = 188;
        public const float FooterY = YourY + 272, FooterH = 56;
        public const float InnerX = Left + Pad, InnerRight = Right - Pad;
        public const float EndRoundX = InnerRight - 240, EndRoundY = FooterY + 4, EndRoundW = 240, EndRoundH = 48;
        const float RoundCol = 118, RoundGap = 6, LabelColX = Left + 16, RoundColsX = Left + 138, TotalColX = RoundColsX + 5 * RoundCol + 4 * RoundGap + 24;

        static CoinType? BuffCoinType(Buff buff)
        {
            if (buff.TargetType.HasValue) return buff.TargetType;
            return Enum.TryParse(buff.Kind, true, out CoinType type) ? type : (CoinType?)null;
        }

        static Rgba SideColor(string side) => side == Side.Heads ? C.Blue : side == Side.Tie ? C.Purple : C.Red;
        static string SideWord(string side) => side == Side.Tie ? L("EDGE") : L(side == Side.Heads ? "HEADS" : "TAILS");
        static string SideLetter(string side) => side == Side.Tie ? L("EDGE") : L(side == Side.Heads ? "H" : "T");
        static Rgba Tint(Rgba toward, float amount) => new Rgba(C.Ink.R + (toward.R - C.Ink.R) * amount, C.Ink.G + (toward.G - C.Ink.G) * amount, C.Ink.B + (toward.B - C.Ink.B) * amount);

        static void Cell(float x, float y, float w, float h, Rgba fill, Rgba edge)
        {
            Color(fill);
            Gfx.Rectangle(true, x, y, w, h);
            Outline(x, y, w, h, edge);
        }

        // ---- top bar ----

        static void DrawBar(GameState g, Encounter e)
        {
            string fight = e.Endless != null ? L("FIGHT %d", g.EncounterIndex) : L("FIGHT %d / 8", g.EncounterIndex);
            Text(fight, Left, BarY + 6, Ui.F20, C.Muted);
            string name = e.Endless != null ? L("ENDLESS %d", e.Endless.Value) : e.Boss ? L("THE HOUSE") : Lang.Upper(L(e.Name));
            Text("-", Left + Ui.F20.GetWidth(fight) + 12, BarY + 6, Ui.F20, C.Muted);
            Text(name, Left + Ui.F20.GetWidth(fight) + 32, BarY + 6, Ui.F20, e.Boss ? C.Red : C.Face);
            Button("MENU", Right - 100, BarY, 100, BarH, C.PanelLight, A.OpenMenu, true, null, Ui.F16);
            var stats = new[] { ("gold", GameText.Num(g.Player.Gold), C.Gold), ("energy", GameText.Num(g.Player.Energy), C.Blue) };
            float x = Right - 124;
            for (int i = stats.Length - 1; i >= 0; i--)
            {
                x -= 30 + Ui.F20.GetWidth(stats[i].Item2);
                ImageAt(Ui.UiImages[stats[i].Item1], x, BarY + 3, 26);
                Text(stats[i].Item2, x + 32, BarY + 6, Ui.F20, stats[i].Item3);
                x -= 22;
            }
        }

        // ---- enemy row ----

        // One enemy coin: face down ("?") until it has flipped, then its coin, side and points.
        static void EnemySlot(EnemyFlip flip, float x, float y, float w, float h)
        {
            if (flip == null)
            {
                Cell(x, y, w, h, C.SlotDk, C.Line);
                Centered("?", x, y + (h - Ui.F32.Height) / 2f, w, Ui.F32, C.Muted);
                return;
            }
            var tint = SideColor(flip.Result);
            Cell(x, y, w, h, C.Card, tint);
            float size = Math.Min(36, w - 12);
            CoinImage(flip.CoinId, x + (w - size) / 2, y + 5, size);
            string label = flip.Result == Side.Tie ? L("EDGE") : L(flip.Result == Side.Heads ? "H" : "T");
            string points = flip.Points > 0 ? " +" + GameText.Num(flip.Points) : " 0";
            Gfx.SetScissor(x + 2, y, w - 4, h);
            Centered(label + points, x, y + h - 21, w, Ui.F16, flip.Points > 0 ? tint : C.Muted);
            Gfx.ClearScissor();
            CoinHover(flip.CoinId, x, y, w, h);
        }

        static void DrawEnemy(Encounter e)
        {
            var d = Game.EnemyOf(G);
            Box(Left, EnemyY, Width, EnemyH, C.PanelDk);
            Outline(Left, EnemyY, Width, EnemyH, d != null && d.Boss ? C.Red : d != null && d.Elite ? C.Purple : C.Line);
            if (d == null)
            {
                Text("NO ENEMY", InnerX, EnemyY + 14, Ui.F20, C.Muted);
                return;
            }
            const float infoW = 330;
            Text(Lang.Upper(L(d.Name)), InnerX, EnemyY + 12, Ui.F20, d.Boss ? C.Red : d.Elite ? C.Purple : C.Gold);
            Gfx.SetFont(Ui.F16);
            Color(C.Muted);
            Gfx.SetScissor(InnerX, EnemyY + 38, infoW, 40);
            Gfx.Printf(L(d.Description), InnerX, EnemyY + 38, infoW);
            Gfx.ClearScissor();
            int total = e.EnemyPouch.Count + e.EnemyDiscard.Count;
            Text(L("FLIPS %d A ROUND", e.EnemyDraw), InnerX, EnemyY + 84, Ui.F16, C.Face);
            Text(L("POUCH %d", total), InnerX + Ui.F16.GetWidth(L("FLIPS %d A ROUND", e.EnemyDraw)) + 24, EnemyY + 84, Ui.F16, C.Face);
            TextHover(Lang.Upper(L(d.Name)), EnemyTip(e, d), InnerX, EnemyY + 8, infoW, EnemyH - 16);

            // its coins: the last round's results, or face-down slots before it has flipped at all
            float slotsX = InnerX + infoW + 40, slotsW = InnerRight - slotsX;
            bool revealed = e.EnemyFlips.Count > 0;
            string caption = revealed ? L("LAST FLIPS  -  ROUND %d", e.EnemyRoundScores.Count > 0 ? e.EnemyRoundScores.Count : Math.Max(1, e.Round - 1)) : L("HIDDEN UNTIL YOU END THE ROUND");
            Text(caption, slotsX, EnemyY + 10, Ui.F16, C.Muted);
            int count = revealed ? e.EnemyFlips.Count : e.EnemyDraw;
            bool bonus = e.EnemyRoundPoints > 0;
            int cells = count + (bonus ? 1 : 0);
            float gap = 8, slotH = 68, slotY = EnemyY + EnemyH - slotH - 12;
            float slotW = Math.Min(84, (slotsW - (Math.Max(1, cells) - 1) * gap) / Math.Max(1, cells));
            for (int i = 0; i < count; i++) EnemySlot(revealed ? e.EnemyFlips[i] : null, slotsX + i * (slotW + gap), slotY, slotW, slotH);
            if (bonus)
            {
                float bx = slotsX + count * (slotW + gap);
                Cell(bx, slotY, slotW, slotH, C.SlotDk, C.Orange);
                Centered("+" + GameText.Num(e.EnemyRoundPoints), bx, slotY + 8, slotW, Ui.F20, C.Orange);
                Gfx.SetScissor(bx + 2, slotY, slotW - 4, slotH);
                Centered(L("BONUS"), bx, slotY + slotH - 24, slotW, Ui.F16, C.Orange);
                Gfx.ClearScissor();
                TextHover(L("ROUND BONUS"), BonusTip(e, d), bx, slotY, slotW, slotH);
            }
        }

        // Hovering the enemy: the fixed pouch it flips from, and where its per-round points come from.
        static string EnemyTip(Encounter e, EnemyDef d)
        {
            var counts = new List<string>();
            var seen = new Dictionary<string, int>();
            foreach (var id in d.Pouch)
            {
                if (!seen.ContainsKey(id)) { seen[id] = 0; counts.Add(id); }
                seen[id]++;
            }
            var parts = new List<string>();
            foreach (var id in counts) parts.Add(seen[id] + "x " + Lang.CoinName(id));
            string text = L("ITS POUCH") + ": " + string.Join(", ", parts);
            if (e.EnemyRoundPoints > 0) text += "\n" + BonusTip(e, d);
            return text;
        }

        static string BonusTip(Encounter e, EnemyDef d)
        {
            double fromStage = e.EnemyRoundPoints - d.RoundPoints;
            return L("+%d POINTS EVERY ROUND  (TRAIT %d, STAGE OR ENDLESS %d)", e.EnemyRoundPoints, d.RoundPoints, fromStage);
        }

        // ---- scoreboard ----

        static void DrawBoard(Encounter e)
        {
            Box(Left, BoardY, Width, BoardH, C.PanelDk);
            Outline(Left, BoardY, Width, BoardH, e.Cleared ? C.Green : C.Line);
            float headY = BoardY + 12, enemyRowY = BoardY + 40, youRowY = enemyRowY + 72, rowH = 66;
            Text(e.Cleared ? L("FIGHT WON") : L("ROUND %d / %d", e.Round, Game.Rounds), LabelColX, headY, Ui.F16, e.Cleared ? C.Green : C.Gold);
            int played = e.Cleared ? e.Round : e.Round - 1; // rounds finished
            for (int r = 1; r <= Game.Rounds; r++)
            {
                float x = RoundColsX + (r - 1) * (RoundCol + RoundGap);
                bool current = r == e.Round && !e.Cleared, done = r <= played;
                Centered("R" + r, x, headY, RoundCol, Ui.F16, current ? C.Gold : done ? C.Face : C.Muted);
                bool have = r - 1 < e.RoundScores.Count && r - 1 < e.EnemyRoundScores.Count;
                DrawBoardCell(x, enemyRowY, RoundCol, rowH, C.Red, current, done, have ? e.EnemyRoundScores[r - 1] : (double?)null,
                    current ? (e.EnemyScore - e.RoundStartEnemy > 0 ? "+" + GameText.Num(e.EnemyScore - e.RoundStartEnemy) : "?") : null);
                DrawBoardCell(x, youRowY, RoundCol, rowH, C.Green, current, done, have ? e.RoundScores[r - 1] : (double?)null,
                    current ? GameText.Num(e.Scored - e.RoundStartScored) : null);
            }
            Text("ENEMY", LabelColX, enemyRowY + (rowH - Ui.F20.Height) / 2f, Ui.F20, C.Red);
            Text("YOU", LabelColX, youRowY + (rowH - Ui.F20.Height) / 2f, Ui.F20, C.Green);

            float totalW = InnerRight - TotalColX;
            string lead = e.Scored > e.EnemyScore ? L("YOU") + " +" + GameText.Num(e.Scored - e.EnemyScore)
                : e.EnemyScore > e.Scored ? L("ENEMY") + " +" + GameText.Num(e.EnemyScore - e.Scored) : e.Scored > 0 ? L("TIED  -  A TIE LOSES") : L("TIED");
            Text("TOTAL", TotalColX, headY, Ui.F16, C.Muted);
            string leadText = lead;
            Text(leadText, TotalColX + totalW - Ui.F16.GetWidth(L(leadText)), headY, Ui.F16, e.Scored > e.EnemyScore ? C.Green : e.EnemyScore > e.Scored ? C.Red : C.Muted);
            bool ahead = e.Scored > e.EnemyScore, behind = e.EnemyScore >= e.Scored && e.EnemyScore > 0;
            Cell(TotalColX, enemyRowY, totalW, rowH, behind ? Tint(C.Red, .32f) : C.SlotDk, behind ? C.Red : C.Line);
            Centered(GameText.Num(e.EnemyScore), TotalColX, enemyRowY + (rowH - Ui.F48.Height) / 2f, totalW, Ui.F48, behind ? C.Face : C.Muted);
            Cell(TotalColX, youRowY, totalW, rowH, ahead ? Tint(C.Green, .32f) : C.SlotDk, ahead ? C.Green : C.Line);
            Centered(GameText.Num(e.Scored), TotalColX, youRowY + (rowH - Ui.F48.Height) / 2f, totalW, Ui.F48, ahead ? C.Face : C.Muted);
        }

        static void DrawBoardCell(float x, float y, float w, float h, Rgba side, bool current, bool done, double? points, string running)
        {
            string text = "-";
            var tint = C.Muted;
            Rgba fill = C.SlotDk, edge = Tint(C.Line, .6f);
            if (current) { fill = Tint(side, .16f); edge = C.Gold; text = running ?? "?"; tint = C.Gold; }
            else if (done && points.HasValue) { fill = Tint(side, .34f); edge = side; text = GameText.Num(points.Value); tint = C.Face; }
            else if (done) { edge = C.Line; }
            Cell(x, y, w, h, fill, edge);
            if (current) Outline(x - 2, y - 2, w + 4, h + 4, C.Gold);
            Centered(text, x, y + (h - Ui.F48.Height) / 2f, w, Ui.F48, tint);
        }

        // ---- your area ----

        // What you flipped this round, newest last; older entries drop off when the line is full.
        static void DrawStrip(Encounter e, float maxX)
        {
            string label = L("THIS ROUND") + ":";
            Text(label, InnerX, StripY, Ui.F16, C.Gold);
            float x = InnerX + Ui.F16.GetWidth(label) + 14;
            var log = e.RoundLog;
            if (log.Count == 0) { Text("NOTHING FLIPPED YET", x, StripY, Ui.F16, C.Muted); return; }
            float EntryWidth(RoundFlip f) => Ui.F16.GetWidth(Lang.Upper(Lang.CoinName(f.CoinId))) + Ui.F16.GetWidth(SideLetter(f.Result)) + Ui.F16.GetWidth(Points(f)) + 24;
            float avail = maxX - x - 40, used = 0;
            int first = log.Count;
            while (first > 0 && used + EntryWidth(log[first - 1]) + 16 <= avail) { first--; used += EntryWidth(log[first]) + 16; }
            if (first > 0) { string more = "+" + first; Text(more, x, StripY, Ui.F16, C.Muted); x += Ui.F16.GetWidth(more) + 14; }
            for (int i = first; i < log.Count; i++)
            {
                var f = log[i];
                string name = Lang.Upper(Lang.CoinName(f.CoinId));
                Text(name, x, StripY, Ui.F16, C.Face);
                x += Ui.F16.GetWidth(name) + 8;
                Text(SideLetter(f.Result), x, StripY, Ui.F16, SideColor(f.Result));
                x += Ui.F16.GetWidth(SideLetter(f.Result)) + 8;
                string pts = Points(f);
                Text(pts, x, StripY, Ui.F16, f.Points > 0 ? C.Green : f.Penalty > 0 ? C.Red : C.Muted);
                x += Ui.F16.GetWidth(pts) + 16;
            }
        }

        static string Points(RoundFlip f) => f.Points > 0 ? "+" + GameText.Num(f.Points) : f.Penalty > 0 ? L("ENEMY") + " +" + GameText.Num(f.Penalty) : "0";

        // Combo multiplier and the level's modifier, right-aligned on the strip line.
        static float DrawCombo(Encounter e)
        {
            double len = e.ComboLen;
            double mult = Math.Min(e.ComboCap, 1 + e.ComboStep * (Math.Max(len, 1) - 1));
            var tint = len < 2 ? C.Muted : e.ComboSide == Side.Heads ? C.Blue : C.Red;
            string label = L("COMBO"), value = GameText.Format("x%.2f", mult);
            string modifier = e.Modifier != null && Game.Modifiers.ContainsKey(e.Modifier) ? Lang.Upper(Lang.ModifierName(e.Modifier)) : null;
            float modW = modifier != null ? Ui.F16.GetWidth(modifier) + 28 : 0;
            float comboW = Ui.F16.GetWidth(label) + 10 + Ui.F20.GetWidth(value);
            float x = InnerRight - modW - comboW;
            Text(label, x, StripY + 1, Ui.F16, tint);
            Text(value, x + Ui.F16.GetWidth(label) + 10, StripY - 2, Ui.F20, len < 2 ? C.Muted : C.Gold);
            string streak = len >= 1 ? L("COMBO  %s x%d", L(e.ComboSide == Side.Heads ? "HEADS" : "TAILS"), len) : L("COMBO");
            TextHover(streak, L("Repeated results raise the multiplier and build an unbanked pot. BANK locks it in as gold."), x - 4, StripY - 4, comboW + 8, 28);
            if (modifier != null)
            {
                float mx = InnerRight - modW + 28;
                Text("-", mx - 18, StripY, Ui.F16, C.Muted);
                Text(modifier, mx, StripY, Ui.F16, C.Orange);
                TextHover(modifier, Lang.ModifierDescription(e.Modifier), mx - 22, StripY - 4, modW, 28);
            }
            return x;
        }

        static string BuffLabel(Buff buff)
        {
            if (buff.Kind == "mult") return L("BUFF x%d  (%d LEFT)", buff.Amount, buff.Left);
            if (buff.Kind == "odds") return L("BUFF +%d%% HEADS  (%d LEFT)", Pct(buff.Amount), buff.Left);
            if (buff.Kind == "swap") return L("BUFF: NEXT COIN SWAPS SIDES");
            if (buff.Kind == "heads") return L("BUFF: NEXT COIN LANDS HEADS");
            if (buff.Kind == "effect" && buff.AppliedEffect != null)
            {
                string target = buff.TargetType.HasValue ? L("NEXT %s", L(buff.TargetType.Value.ToString().ToUpperInvariant())) : L("NEXT COIN");
                string effect = EffectDescription(new[] { buff.AppliedEffect });
                return buff.AppliesOn.HasValue
                    ? L("BUFF %s ON %s: %s (%d LEFT)", target, L(buff.AppliesOn.Value.ToString().ToUpperInvariant()), effect, buff.Left)
                    : L("BUFF %s: %s (%d LEFT)", target, effect, buff.Left);
            }
            return L("BUFF %s  (%d LEFT)", L(buff.Kind.ToUpper()), buff.Left);
        }

        // One line under the strip: re-flip/keep while a result waits, else active buffs; luck, shield, combo pot and the discard toggle on the right.
        static void DrawStatus(GameState g, Encounter e, bool busy, bool canAct)
        {
            float right = InnerRight;
            if (canAct && e.BankDiscards > 0)
            {
                right -= 190;
                Button(Ui.DiscardMode ? "DISCARD MODE: ON" : "DISCARD MODE: OFF", right, StatusY, 190, 26, Ui.DiscardMode ? C.Orange : C.PanelLight, () => A.ToggleDiscardMode(), true, null, Ui.F16);
                right -= 14;
            }
            if (e.ComboPot > 0)
            {
                if (!busy && Game.CanBankCombo(g) && !e.Cleared)
                {
                    right -= 130;
                    Button(L("BANK %dG", e.ComboPot), right, StatusY, 130, 26, C.Green, A.BankCombo, true, null, Ui.F16);
                }
                else
                {
                    string pot = L("POT %dG", e.ComboPot);
                    right -= Ui.F16.GetWidth(pot);
                    Text(pot, right, StatusY + 4, Ui.F16, C.Gold);
                    TextHover(pot, L("BREAK LOSES POT"), right, StatusY, Ui.F16.GetWidth(pot), 26);
                }
                right -= 14;
            }
            if (e.Shield > 0)
            {
                string shield = L("SHIELD %d", e.Shield);
                right -= Ui.F16.GetWidth(shield);
                Text(shield, right, StatusY + 4, Ui.F16, C.Green);
                right -= 14;
            }
            if (e.TailsRun > 0)
            {
                string luck = L("LUCK +%d%%", (int)Math.Round(Game.Pity(e) * 100)); // visible pity bonus
                right -= Ui.F16.GetWidth(luck);
                Text(luck, right, StatusY + 4, Ui.F16, C.Green);
                right -= 14;
            }

            float x = InnerX;
            if (Ui.Deciding)
            {
                Button("RE-FLIP (1 ENERGY)", x, StatusY, 230, 26, C.Orange, A.Reflip, true, null, Ui.F16);
                Button("KEEP", x + 242, StatusY, 100, 26, C.Green, A.Keep, true, null, Ui.F16);
                x += 360;
                var result = g.Pending;
                if (result != null && result.Altered != null && result.Raw != null)
                {
                    string altered = L("ROLLED %s  >  %s  (%s)", Lang.Upper(L(result.Raw)), Lang.Upper(L(result.Result)), L(result.Altered));
                    Gfx.SetScissor(x, StatusY, right - x - 8, 26);
                    Text(altered, x, StatusY + 4, Ui.F16, C.Orange);
                    Gfx.ClearScissor();
                }
                return;
            }
            if (e.Hand.Count == 0 && !e.Cleared && slots.Count > 0 && g.Pending == null)
            {
                Text("NO COINS IN HAND  -  END THE ROUND", x, StatusY + 4, Ui.F16, C.Orange);
                return;
            }
            Gfx.SetScissor(x, StatusY, Math.Max(0, right - x - 8), 26);
            for (int i = 0; i < e.Buffs.Count; i++)
            {
                string label = BuffLabel(e.Buffs[i]);
                float w = Ui.F16.GetWidth(L(label));
                if (x + w > right - 8 && i > 0) { Text(L("+%d MORE BUFFS", e.Buffs.Count - i), x, StatusY + 4, Ui.F16, C.Orange); break; }
                CoinType? targetType = BuffCoinType(e.Buffs[i]);
                Text(label, x, StatusY + 4, Ui.F16, targetType.HasValue ? CoinTypeColor(targetType.Value) : C.Orange);
                x += w + 24;
            }
            Gfx.ClearScissor();
        }

        // ---- the hand ----

        // The cards of this round keep their place: a flipped coin leaves the hand but its card stays, showing the flip and then the result.
        static Encounter slotsOwner;
        static int slotsRound;
        static readonly List<int> slots = new List<int>();

        static void SyncSlots(GameState g, Encounter e)
        {
            if (slotsOwner != e || slotsRound != e.Round)
            {
                slotsOwner = e; slotsRound = e.Round; slots.Clear();
                foreach (var f in e.RoundLog) if (!slots.Contains(f.Uid)) slots.Add(f.Uid);
                if (g.Pending != null && !slots.Contains(g.Pending.Uid)) slots.Add(g.Pending.Uid);
            }
            foreach (int uid in e.Hand) if (!slots.Contains(uid)) slots.Add(uid);
            slots.RemoveAll(uid => !e.Hand.Contains(uid) && (g.Pending == null || g.Pending.Uid != uid) && !e.RoundLog.Exists(f => f.Uid == uid));
        }

        // The coin turning over inside its card.
        static void DrawFlipAnimation(float cx, float cy, float radius, float labelY, float w)
        {
            var anim = Ui.FlipAnimation;
            double progress = Math.Min(1, anim.Elapsed / anim.Duration);
            // quartic ease-out so the spin slows to a tense stop; even half-turn = Tails up, odd = Heads up
            int turns = anim.Outcome == Side.Heads ? 9 : 10;
            double phase = turns * (1 - Math.Pow(1 - progress, 4));
            float squash = (float)Math.Max(.06, Math.Abs(Math.Cos(phase * Math.PI)));
            bool heads = (long)Math.Floor(phase + .5) % 2 == 1; // the side changes at the thin edge-on moments
            float lift = (float)(radius * .3 * Math.Sin(Math.Min(1, progress / .75) * Math.PI));
            Gfx.Push();
            Gfx.Translate(cx, cy - lift);
            Gfx.Scale(squash, 1);
            Color(C.White);
            var image = Ui.CoinImages[anim.Id];
            Gfx.Draw(image, -radius, -radius, 2 * radius / image.Width, 2 * radius / image.Height);
            Gfx.Pop();
            string word = anim.Outcome == Side.Tie ? "EDGE" : L(heads ? "HEADS" : "TAILS");
            Centered(word, cx - w / 2, labelY, w, Ui.F20, heads ? C.Blue : C.Red);
        }

        static string ResultNote(double? gained, double? penalty, out Rgba tint)
        {
            tint = C.Muted;
            if (gained > 0 && penalty > 0) { tint = C.Purple; return L("+%d PTS / ENEMY +%d", gained.Value, penalty.Value); }
            if (gained > 0) { tint = C.Green; return L("+%d POINTS", gained.Value); }
            if (penalty > 0) { tint = C.Red; return L("ENEMY +%d", penalty.Value); }
            return gained != null ? "NO POINTS" : "";
        }

        // A card whose coin has flipped (or is flipping): the coin, the side it landed on and what it was worth.
        static void DrawResultCard(float x, float y, float w, float h, string coinId, string outcome, string note, Rgba noteTint, bool live)
        {
            float radius = Math.Min(48, (w - 20) / 2);
            float cx = x + w / 2, cy = y + 12 + radius;
            float labelY = cy + radius + 10, noteY = labelY + Ui.F20.Height + 2;
            var accent = outcome != null ? SideColor(outcome) : C.Gold;
            Box(x, y, w, h, live ? C.Card : C.SlotDk);
            Outline(x, y, w, h, live ? C.Gold : accent);
            if (Ui.FlipAnimation != null && live)
            {
                DrawFlipAnimation(cx, cy, radius, labelY, w);
                return;
            }
            Color(C.PanelDk);
            Gfx.Circle(true, cx, cy, radius + 4);
            CoinImage(coinId, cx - radius, cy - radius, 2 * radius);
            if (outcome == null) return;
            Centered(outcome == Side.Tie ? "EDGE" : Lang.Upper(outcome), x, labelY, w, Ui.F20, accent);
            Gfx.SetFont(Ui.F16);
            Color(noteTint);
            Gfx.SetScissor(x + 4, noteY, w - 8, h - (noteY - y) - 4);
            Gfx.Printf(L(note), x + 6, noteY, w - 12, Align.Center);
            Gfx.ClearScissor();
        }

        static void DrawHandCard(Encounter e, int uid, float x, float y, float w, float h, bool canAct, bool discarding)
        {
            var owned = Game.GetCoin(G, uid);
            var def = Content.Coins[owned.Id];
            var odds = Game.GetOdds(G, owned);
            bool afford = def.EnergyCost <= G.Player.Energy;
            bool click = canAct && (afford || discarding);
            Ui.Mouse(out float mx, out float my);
            bool hover = click && mx >= x && mx <= x + w && my >= y && my <= y + h;
            Box(x, y, w, h, hover ? C.Marked : afford ? C.Card : C.SlotDk);
            Outline(x, y, w, h, discarding ? C.Orange : hover ? C.Face : click ? C.Gold : C.Line);
            const float pad = 10;
            bool wide = w >= 170;
            float img = wide ? 64 : Math.Min(44, w - 2 * pad);
            float textX = wide ? x + pad + img + 8 : x + pad, textW = wide ? w - 2 * pad - img - 8 : w - 2 * pad, textY = wide ? y + pad : y + pad + img + 4;
            CoinImage(owned, wide ? x + pad : x + (w - img) / 2, y + pad, img);
            Gfx.SetFont(Ui.F16);
            Color(afford ? C.Face : C.Muted);
            Gfx.SetScissor(textX, textY, textW, 36);
            Gfx.Printf(Lang.Upper(Lang.CoinName(owned.Id)), textX, textY, textW);
            Gfx.ClearScissor();
            float oddsY = textY + (wide ? 40 : 20);
            string odd = L("%d%% H", Pct(odds.Heads)) + (odds.Edge > 0 ? "  " + L("%d%% E", Pct(odds.Edge)) : "");
            Gfx.SetScissor(textX, oddsY, textW, 20);
            Text(odd, textX, oddsY, Ui.F16, C.Gold);
            Gfx.ClearScissor();
            if (def.EnergyCost > 0) Text("E" + def.EnergyCost, x + w - 8 - Ui.F16.GetWidth("E" + def.EnergyCost), y + 6, Ui.F16, afford ? C.Orange : C.Red);
            float ey = wide ? y + pad + img + 10 : oddsY + 24;
            Gfx.SetScissor(x + 4, ey, w - 8, y + h - ey - 4);
            Gfx.SetFont(Ui.F16);
            if (odds.Heads > 0)
            {
                string heads = L("H") + " " + Effects(def.Heads);
                Color(C.Blue);
                Gfx.Printf(heads, x + pad, ey, w - 2 * pad);
                ey += Ui.F16.GetWrap(heads, w - 2 * pad).Count * Ui.F16.Height + 2;
            }
            if (odds.Heads < 1 - odds.Edge)
            {
                Color(C.Red);
                Gfx.Printf(L("T") + " " + Effects(def.Tails), x + pad, ey, w - 2 * pad);
            }
            Gfx.ClearScissor();
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
                Gfx.Rectangle(true, x + w - 6, y + 4, 4, h - 8);
            }
            CoinHover(owned.Id, x, y, w, h, odds.Heads, tieProbability: odds.Edge);
            if (click) AddButton(x, y, w, h, discarding ? (Action)(() => A.DiscardCoin(uid)) : () => A.FlipCoin(uid), "HAND COIN");
        }

        static void DrawHand(GameState g, Encounter e, bool canAct, bool busy)
        {
            SyncSlots(g, e);
            bool discarding = canAct && e.BankDiscards > 0 && Ui.DiscardMode;
            int n = slots.Count;
            if (n == 0)
            {
                Centered(e.Cleared ? "" : "NO COINS IN HAND  -  END THE ROUND", InnerX, CardsY + CardsH / 2 - 10, InnerRight - InnerX, Ui.F20, C.Muted);
                return;
            }
            float gap = n > 5 ? 8 : 12, avail = InnerRight - InnerX;
            float w = n <= 5 ? (avail - 4 * 12) / 5 : (avail - (n - 1) * gap) / n;
            float x0 = InnerX + (avail - (n * w + (n - 1) * gap)) / 2;
            for (int i = 0; i < n; i++)
            {
                int uid = slots[i];
                float x = x0 + i * (w + gap);
                if (g.Pending != null && g.Pending.Uid == uid)
                {
                    var p = g.Pending;
                    string coinId = Game.GetCoin(g, uid).Id;
                    string outcome = busy ? null : p.Final ?? p.Result;
                    string note = "";
                    var noteTint = C.Muted;
                    if (!busy) note = Ui.Deciding ? "RE-FLIP OR KEEP" : "APPLYING...";
                    DrawResultCard(x, CardsY, w, CardsH, coinId, outcome, note, noteTint, true);
                }
                else if (e.Hand.Contains(uid)) DrawHandCard(e, uid, x, CardsY, w, CardsH, canAct, discarding);
                else
                {
                    var f = e.RoundLog.FindLast(l => l.Uid == uid);
                    if (f == null) continue;
                    string note = ResultNote(f.Points, f.Penalty, out var noteTint);
                    DrawResultCard(x, CardsY, w, CardsH, f.CoinId, f.Result, note, noteTint, false);
                }
            }
        }

        // ---- footer ----

        static void DrawFooter(GameState g, Encounter e, bool busy, bool canAct)
        {
            Color(C.Line);
            Gfx.Rectangle(true, InnerX, FooterY - 8, InnerRight - InnerX, 2);
            Text(L("POUCH %d   DISCARD %d", e.Pouch.Count, e.Discard.Count), InnerX, FooterY + 4, Ui.F16, C.Face);

            string hint = null;
            if (e.Cleared) hint = "Fight won. Move on when you are ready.";
            else if (e.Hand.Count > 0 && canAct)
            {
                bool any = false;
                foreach (int uid in e.Hand) if (Game.FlipCost(g, uid) <= g.Player.Energy) any = true;
                if (!any) hint = L("TOO LITTLE ENERGY: END THE ROUND");
            }
            if (g.Peek != null)
            {
                var names = new List<string>();
                foreach (int uid in g.Peek) names.Add(Lang.Upper(Lang.CoinName(Game.GetCoin(g, uid).Id)));
                hint = L("NEXT: %s", string.Join(", ", names));
            }
            if (hint != null)
            {
                Gfx.SetFont(Ui.F16);
                Color(C.Muted);
                Gfx.SetScissor(InnerX, FooterY + 26, 300, 30);
                Gfx.Printf(L(hint), InnerX, FooterY + 26, 300);
                Gfx.ClearScissor();
            }

            // chips (one-use helpers): three slots
            Ui.Mouse(out float mx, out float my);
            bool usable = Items.CanUse(g) && !busy;
            float cx = InnerX + 330;
            Text("CHIPS", cx, FooterY + 20, Ui.F16, C.Gold);
            cx += Ui.F16.GetWidth(L("CHIPS")) + 12;
            for (int slot = 0; slot < Items.Max; slot++)
            {
                float x = cx + slot * 64, y = FooterY;
                string id = slot < g.Items.Count ? g.Items[slot] : null;
                if (id == null)
                {
                    Cell(x, y, 56, 56, C.SlotDk, Tint(C.Line, .6f));
                    continue;
                }
                bool over = mx >= x && mx <= x + 56 && my >= y && my <= y + 56;
                float lift = usable && over ? -3 : 0;
                Box(x, y + lift, 56, 56, usable ? C.Card : C.SlotDk);
                Outline(x, y + lift, 56, 56, usable ? C.Orange : C.Line);
                ImageAt(Ui.ItemImages[id], x + 12, y + 3 + lift, 32);
                Centered(Lang.ItemShort(id), x, y + 37 + lift, 56, Ui.F16, usable ? C.Face : C.Muted);
                int s = slot;
                if (usable) AddButton(x, y, 56, 56, () => A.UseItem(s), "ITEM");
                TextHover(Lang.Upper(Lang.ItemName(id)), Lang.ItemDescription(id), x, y, 56, 56);
            }

            // prizes (augments, the level's encounter)
            int prizeCount = g.Augments.Count + (g.RunEncounter != null ? 1 : 0);
            if (prizeCount > 0)
            {
                float px = cx + Items.Max * 64 + 28;
                Text("PRIZES", px, FooterY + 20, Ui.F16, C.Gold);
                RunModifierView.Draw(px + Ui.F16.GetWidth(L("PRIZES")) + 12, FooterY + 6, 44);
            }

            if (e.Cleared)
                IconButton(g.Map != null && !g.Endless ? "CONTINUE" : "OPEN SHOP", Ui.UiImages["open_shop"], EndRoundX, EndRoundY, EndRoundW, EndRoundH, C.Green, A.OpenShop,
                    !busy && g.Pending == null);
            else
                IconButton("END ROUND", Ui.UiImages["next_coin"], EndRoundX, EndRoundY, EndRoundW, EndRoundH, C.Blue, A.EndRound, !busy);
        }

        public static void Draw()
        {
            var g = G;
            var e = g.Encounter;
            Frame(null);
            bool busy = Ui.FlipAnimation != null;
            bool canAct = !busy && !e.Cleared && (g.Pending == null || Ui.Deciding || Ui.ResolveTimer > 0);

            DrawBar(g, e);
            DrawEnemy(e);
            DrawBoard(e);

            Box(Left, YourY, Width, YourH, C.PanelDk);
            Outline(Left, YourY, Width, YourH, C.Line);
            float comboX = DrawCombo(e);
            DrawStrip(e, comboX);
            SyncSlots(g, e);
            DrawStatus(g, e, busy, canAct);
            DrawHand(g, e, canAct, busy);
            DrawFooter(g, e, busy, canAct);
        }
    }
}
