using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    // Full-screen collection inspection. Only presentation state lives here; coin rules stay in CoinDef.
    public static class CoinDetailView
    {
        const float InfoX = 572, InfoW = 616, ViewTop = 221, ViewBottom = 698;
        static CoinDef selectedCoin;
        static float scroll, maxScroll;

        public static CoinDef SelectedCoin => selectedCoin;

        public static void Open(CoinDef coin)
        {
            if (coin == null) return;
            selectedCoin = coin;
            scroll = maxScroll = 0;
            Ui.Screen = UiScreen.CoinDetail;
        }

        public static void Close() => Ui.Screen = UiScreen.Collection;

        static void Scroll(float amount) => scroll = Math.Max(0, Math.Min(maxScroll, scroll + amount));

        static float Description(string title, string body, float y, Rgba tint)
        {
            int lines = Ui.F16.GetWrap(body, InfoW - 58).Count;
            float height = 45 + Math.Max(1, lines) * 18;
            Box(InfoX, y, InfoW, height, C.Slot);
            Color(tint); Gfx.Rectangle(true, InfoX, y, 4, height);
            Text(title, InfoX + 17, y + 7, Ui.F20, tint);
            Gfx.SetFont(Ui.F16); Color(C.Face);
            Gfx.Printf(body, InfoX + 20, y + 34, InfoW - 40);
            return y + height + 9;
        }

        static float Outcome(CoinDef coin, string side, double chance, float y, Rgba tint)
        {
            if (chance <= 0) return y;
            string description = CoinOutcomeDescription(coin, side);
            string mastery = CoinMasteryOutcomeDescription(coin, side);
            string title = L(side == Side.Tie ? "EDGE" : side == Side.Heads ? "HEADS" : "TAILS") +
                "  " + Math.Floor(chance * 100 + .5) + "%";
            if (mastery == null) return Description(title, description, y, tint);
            int baseLines = Ui.F16.GetWrap(description, InfoW - 58).Count;
            int bonusLines = Ui.F16.GetWrap(mastery, InfoW - 67).Count;
            float height = 55 + Math.Max(1, baseLines) * 18 + bonusLines * 18;
            Box(InfoX, y, InfoW, height, C.Slot);
            Color(tint); Gfx.Rectangle(true, InfoX, y, 4, height);
            Text(title, InfoX + 17, y + 7, Ui.F20, tint);
            Gfx.SetFont(Ui.F16); Color(C.Face);
            Gfx.Printf(description, InfoX + 20, y + 34, InfoW - 40);
            float bonusY = y + 40 + Math.Max(1, baseLines) * 18;
            Color(C.Purple); Gfx.Rectangle(true, InfoX + 20, bonusY, 3, bonusLines * 18);
            Gfx.SetFont(Ui.F16); Color(C.Purple);
            Gfx.Printf(mastery, InfoX + 31, bonusY, InfoW - 51);
            return y + height + 9;
        }

        static float Characters(CoinDef coin, float y)
        {
            var available = new List<(CharacterDef Def, bool Locked)>();
            foreach (var id in Content.CharacterOrder)
            {
                var character = Content.Characters[id];
                if (character.Pool.Exists(candidate => candidate == coin)) available.Add((character, false));
                else if (character.Locked.Exists(candidate => candidate.Id == coin.Id)) available.Add((character, true));
            }
            if (available.Count == 0) return y;

            int rows = (available.Count + 2) / 3;
            float height = 43 + rows * 128;
            Box(InfoX, y, InfoW, height, C.Slot);
            Color(C.Green); Gfx.Rectangle(true, InfoX, y, 4, height);
            Text(L("CHARACTERS"), InfoX + 17, y + 7, Ui.F20, C.Green);
            for (int index = 0; index < available.Count; index++)
            {
                var character = available[index];
                float x = InfoX + 16 + (index % 3) * 198;
                float cardY = y + 35 + (index / 3) * 128;
                Box(x, cardY, 188, 117, C.PanelDk);
                Outline(x, cardY, 188, 117, C.Line);
                var image = Ui.CharacterImages[character.Def.Id];
                float scale = Math.Min(82f / image.Width, 96f / image.Height);
                float width = image.Width * scale, imageHeight = image.Height * scale;
                Color(C.White, character.Locked ? .45f : 1);
                Gfx.Draw(image, x + 7 + (82 - width) / 2, cardY + 10 + (96 - imageHeight) / 2, scale, scale);
                Gfx.SetFont(Ui.F16); Color(C.Face);
                Gfx.Printf(Lang.CharacterName(character.Def.Id), x + 97, cardY + 19, 83);
                if (character.Locked) Text(L("LOCKED"), x + 97, cardY + 88, Ui.F16, C.Orange);
            }
            return y + height + 9;
        }

        static float DrawDetails(CoinDef coin, float y)
        {
            y = Outcome(coin, Side.Heads, coin.Probability, y, C.Blue);
            y = Outcome(coin, Side.Tie, coin.TieProbability, y, C.Purple);
            y = Outcome(coin, Side.Tails, 1 - coin.Probability - coin.TieProbability, y, C.Red);

            string special = Lang.CoinSpecialRule(coin.Id);
            if (!string.IsNullOrWhiteSpace(special)) y = Description(L("SPECIAL RULE"), L(special), y, C.Gold);

            var buffs = coin.BuffsFor();
            foreach (var buff in buffs)
                y = Description(L("BUFFS"), BuffDescription(buff), y, C.Purple);

            return Characters(coin, y);
        }

        public static void Draw()
        {
            var coin = selectedCoin;
            if (coin == null)
            {
                Close();
                return;
            }

            Frame(null, "BACK", Close);
            Text(L("COIN DETAILS"), 75, 69, Ui.F32, C.Gold);
            Box(68, 126, 470, 592, C.PanelDk);
            Outline(68, 126, 470, 592, RarityColor(coin.Rarity));
            CoinImage(coin, 128, 175, 350);

            int level = Profile.MasteryLevel(Ui.Profile, coin);
            Box(89, 552, 428, 145, C.Slot);
            Text(L("MASTERY LEVEL %d / 3", level), 108, 566, Ui.F20, C.Purple);
            double progressValue = Profile.MasteryProgress(Ui.Profile, coin);
            double stageProgress = 0;
            if (coin.Mastery != null && level < coin.Mastery.Thresholds.Length)
            {
                string progress = L("%s: %s / %s", coin.Mastery.ProgressDescription,
                    GameText.Num(progressValue),
                    GameText.Num(coin.Mastery.Thresholds[level]));
                Gfx.SetFont(Ui.F16); Color(C.Face);
                Gfx.Printf(progress, 108, 603, 388);
                double previous = level == 0 ? 0 : coin.Mastery.Thresholds[level - 1];
                stageProgress = Math.Max(0, Math.Min(1,
                    (progressValue - previous) / (coin.Mastery.Thresholds[level] - previous)));
                Gfx.Printf(L("NEXT: %s", coin.Mastery.Rewards[level]), 108, 661, 388);
            }
            else Text(L("MAX LEVEL"), 108, 610, Ui.F20, C.Gold);
            float filled = (float)((level + stageProgress) / 3);
            Box(108, 633, 388, 17, C.Ink);
            if (filled > 0)
            {
                Color(C.Purple);
                Gfx.Rectangle(true, 111, 636, 382 * filled, 11);
            }
            Outline(108, 633, 388, 17, C.Gold);
            Color(C.Gold);
            Gfx.Rectangle(true, 108 + 388f / 3, 634, 2, 15);
            Gfx.Rectangle(true, 108 + 776f / 3, 634, 2, 15);

            Box(553, 126, 655, 592, C.PanelDk);
            Outline(553, 126, 655, 592, C.Line);
            Text(Lang.CoinName(coin.Id), 575, 138, Ui.F32, C.Face);
            string rarity = coin.Rarity == Rarity.Common ? "Common" : coin.Rarity == Rarity.Uncommon ? "Uncommon" :
                coin.Rarity == Rarity.Rare ? "Rare" : "Epic";
            Text(Lang.Upper(L(rarity)), 576, 177, Ui.F16, RarityColor(coin.Rarity));
            float typeX = 681;
            foreach (var type in coin.Types)
            {
                string name = L(type.ToString().ToUpperInvariant());
                Text(name, typeX, 177, Ui.F16, CoinTypeColor(type));
                typeX += Ui.F16.GetWidth(name) + 12;
            }
            Text(L("COST: %d GOLD", coin.Cost), 576, 198, Ui.F16, C.Gold);
            Text(L("ENERGY: %d", coin.EnergyCost), 785, 198, Ui.F16, C.Gold);
            if (!Ui.Profile.Collected.Contains(coin.Id)) Text(L("UNCOLLECTED"), 979, 198, Ui.F16, C.Muted);

            Gfx.SetScissor(InfoX, ViewTop, InfoW, ViewBottom - ViewTop);
            float end = DrawDetails(coin, ViewTop - scroll);
            Gfx.ClearScissor();
            maxScroll = Math.Max(0, end + scroll - ViewBottom + 5);
            if (scroll > maxScroll) scroll = maxScroll;
            if (maxScroll > 0)
            {
                Button("UP", 1116, 137, 76, 35, C.PanelLight, () => Scroll(-125), scroll > 0);
                Button("DOWN", 1116, 177, 76, 35, C.PanelLight, () => Scroll(125), scroll < maxScroll);
            }
        }
    }
}
