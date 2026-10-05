using System;
using static Tossup.UI.D;

namespace Tossup.UI
{
    public static class RunModifierView
    {
        public static void Draw(float x,float y,float size)
        {
            var g=Ui.Game;int i=0;
            if(g.RunEncounterId!=null&&Game.Encounters.TryGetValue(g.RunEncounterId,out var encounter))
            {
                float at=x+i++*(size+5);if(Ui.EncounterImages.TryGetValue(g.RunEncounterId,out var image))ImageAt(image,at,y,size);else{Box(at,y,size,size,C.PanelDk);Centered("E",at,y+5,size,Ui.F20,C.Gold);}
                TextHover(encounter.Name,encounter.Description,at,y,size,size);
            }
            foreach(var id in g.Augments){float at=x+i++*(size+5);if(Ui.AugmentImages.TryGetValue(id,out var image))ImageAt(image,at,y,size);var d=Game.AugmentDefs[id];string detail=d.Description;if(id=="type_specialist"&&g.AugmentData.TryGetValue(id,out var selected))detail+=" ["+selected.ToUpper()+"]";TextHover(d.Name,detail,at,y,size,size);}
        }
    }

    public static class ContractView
    {
        public static void Draw()
        {
            var g = Ui.Game;
            var e = g.Encounter;
            Frame(null);
            Centered(L("LEVEL %d CONTRACT", g.EncounterIndex), 89, 84, 1443, Ui.F32, C.Gold);
            Centered(L("Choose a challenge for a bonus, or skip it."), 89, 132, 1443, Ui.F20, C.Muted);
            string stageName = e.Endless.HasValue ? L("ENDLESS %d", e.Endless.Value) : L(e.Name);
            Centered(Lang.Upper(stageName), 89, 166, 1443, Ui.F16, C.Face);

            var options = e.ContractOptions ?? new System.Collections.Generic.List<string>();
            for (int i = 0; i < options.Count; i++)
            {
                string id = options[i];
                var d = Game.Contracts[id];
                float x = 203 + i * 418;
                Box(x, 220, 380, 360, C.PanelDk);
                Outline(x, 220, 380, 360, C.Line);
                Centered(L(d.Name), x + 16, 254, 339, Ui.F20, C.Gold);
                Centered(d.RewardText != null ? L(d.RewardText) : L("BONUS +%dG", d.Reward), x + 16, 304, 339,
                    d.RewardText != null ? Ui.F20 : Ui.F32, C.Green);
                Gfx.SetFont(Ui.F16);
                Color(C.Muted);
                Gfx.Printf(L(d.Description), x + 28, 358, 309, Align.Center);
                Centered(L("DRAWBACK"), x + 16, 438, 339, Ui.F16, C.Red);
                Color(C.Red);
                Gfx.Printf(L(d.Drawback), x + 28, 466, 309, Align.Center);
                Button(L("TAKE CONTRACT"), x + 24, 516, 319, 52, C.Blue, () => A.ChooseContract(id));
            }
            Button(L("SKIP CONTRACT"), 633, 638, 354, 54, C.PanelLight, A.SkipContract);
        }
    }

    public static class AugmentView
    {
        public static void Draw()
        {
            var g = Ui.Game;
            Frame(null);
            Centered(L("LEVEL %d AUGMENT", g.AugmentLevel ?? g.EncounterIndex), 89, 82, 1443, Ui.F32, C.Gold);
            if (g.AugmentPending != null) { DrawPending(g); return; }
            Centered(L("Choose a run upgrade or change a coin before this level."), 89, 128, 1443, Ui.F20, C.Muted);
            var options = g.AugmentOptions ?? new System.Collections.Generic.List<string>();
            const float width = 380, height = 390, gap = 38;
            float startX = (Ui.Width - (width * 3 + gap * 2)) / 2;
            for (int i = 0; i < options.Count; i++)
            {
                string id = options[i];
                var d = Game.AugmentDefs[id];
                float x = startX + i * (width + gap);
                Box(x, 205, width, height, C.PanelDk);
                Outline(x, 205, width, height, C.Line);
                Centered(L(d.Name), x + 16, 231, width - 32, Ui.F20, C.Gold);
                if (Ui.AugmentImages.TryGetValue(id, out var image)) ImageAt(image, x + (width - 96) / 2, 275, 96);
                Centered(L(d.Tier.ToUpper() + " TIER"), x + 16, 379, width - 32, Ui.F16, C.Muted);
                Gfx.SetFont(Ui.F16);
                Color(C.Face);
                Gfx.Printf(L(d.Description), x + 28, 418, width - 56, Align.Center);
                Button(L("CHOOSE AUGMENT"), x + 24, 531, width - 48, 48, C.Blue, () => A.ChooseAugment(id));
            }
        }

        static void DrawPending(GameState g)
        {
            var pending = g.AugmentPending;
            var def = Game.AugmentDefs[pending.Id];
            string heading = pending.Id == "epic_windfall" ? "CHOOSE A COIN TO REPLACE" :
                pending.Id == "reforger" ? "CHOOSE A COIN TO REFORGE" :
                pending.Id == "type_specialist" ? "CHOOSE A COIN TYPE" :
                pending.Id == "upgrade_press" ? "CHOOSE A COIN UPGRADE" : def.Name.ToUpper();
            Centered(L(heading), 89, 126, 1443, Ui.F20, C.Gold);
            string subtitle = pending.RewardId != null
                ? L("NEW COIN: %s", Lang.CoinName(pending.RewardId))
                : L(def.Description);
            Centered(subtitle, 89, 158, 1443, Ui.F16, C.Muted);
            if (pending.RewardId != null)
            {
                CoinImage(pending.RewardId, 1461, 140, 42);
                CoinHover(pending.RewardId, 1461, 140, 42, 42);
            }

            var choices = Game.AugmentChoices(g);
            if (choices.Count == 0) return;
            int cols = choices.Count > 12 ? 4 : choices.Count > 6 ? 3 : 2;
            int rows = (choices.Count + cols - 1) / cols;
            const float gap = 16;
            float width = (float)Math.Floor((1468 - (cols - 1) * gap) / cols);
            float height = Math.Min(116, (float)Math.Floor((510 - (rows - 1) * gap) / rows));
            float startX = (Ui.Width - (width * cols + gap * (cols - 1))) / 2;
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                float x = startX + (i % cols) * (width + gap);
                float y = 210 + (i / cols) * (height + gap);
                Box(x, y, width, height, C.PanelDk);
                Outline(x, y, width, height, C.Line);
                bool hasCoin = choice.CoinId != null;
                if (hasCoin)
                {
                    CoinImage(choice.CoinId, x + 12, y + 10, 44);
                    CoinHover(choice.CoinId, x, y, width, height);
                }
                float textX = x + (hasCoin ? 64 : 16);
                float textWidth = width - (textX - x) - 12;
                string title = hasCoin ? Lang.CoinName(choice.CoinId) : L(choice.Title);
                Text(title, textX, y + 8, Ui.F20, C.Gold);
                bool compact = choices.Count > 12;
                string detail = choice.UpgradeName != null
                    ? L(choice.UpgradeName) + (compact ? "" : ": " + L(choice.Detail))
                    : L(choice.Detail);
                Gfx.SetFont(Ui.F16);
                Color(C.Face);
                Gfx.Printf(detail, textX, y + 35, textWidth);
                if (compact && choice.UpgradeName != null) TextHover(L(choice.UpgradeName), L(choice.Detail), x, y, width, height);
                float buttonHeight = Math.Min(28, Math.Max(18, height - 8));
                Button(L("CHOOSE"), x + 12, y + height - buttonHeight - 6, width - 24, buttonHeight, C.Blue,
                    () => A.ChooseAugmentOption(choice.Key));
            }
        }
    }

    public static class EncounterRevealView
    {
        public static void Draw()
        {
            var reveal=Ui.EncounterReveal;var game=Ui.Game;
            if(reveal==null||game==null||game.RunEncounterId==null||!Game.Encounters.TryGetValue(game.RunEncounterId,out var encounter))return;
            Ui.Buttons.Clear();
            float alpha=(float)Math.Max(0,Math.Min(1,reveal.Elapsed/.32));
            float rise=1-(float)Math.Pow(1-alpha,3);float pulse=1+(float)Math.Sin(reveal.Elapsed*5.5)*.025f;
            Color(C.Ink,.88f*alpha);Gfx.Rectangle(true,0,0,1620,800);
            Gfx.Push(); Gfx.Translate(810, 338); Color(C.Gold, .24f * alpha); Gfx.SetLineWidth(3);
            for (int i = 0; i < 20; i++)
            {
                double angle = i * Math.PI / 10 + reveal.Elapsed * .24;
                float inner = 142 + (float)Math.Sin(reveal.Elapsed * 3 + i) * 8, outer = 226 + (float)Math.Sin(reveal.Elapsed * 2.2 + i * .7) * 12;
                Gfx.Line((float)Math.Cos(angle) * inner, (float)Math.Sin(angle) * inner, (float)Math.Cos(angle) * outer, (float)Math.Sin(angle) * outer);
            }
            for (int i = 1; i <= 3; i++) Gfx.Circle(false, 0, 0, 185 + i * 24 + (float)Math.Sin(reveal.Elapsed * 3 - i) * 7);
            Gfx.Pop(); Gfx.SetLineWidth(1);
            float panelY=95+(1-rise)*84;Box(323,panelY,975,545,C.PanelDk);Outline(323,panelY,975,545,C.Gold);Outline(342,panelY+15,937,515,C.Line);
            Centered("RUN ENCOUNTER",373,panelY+34,873,Ui.F20,C.Gold);Centered("ONE RULE FOR THE WHOLE RUN",373,panelY+74,873,Ui.F16,C.Muted);
            if(Ui.EncounterImages.TryGetValue(game.RunEncounterId,out var image))
            {
                float size=190*pulse*(.72f+.28f*rise);
                Gfx.Push(); Gfx.Translate(810,326+(1-rise)*40); Gfx.Rotate((float)Math.Sin(reveal.Elapsed*1.8)*.09f);
                Color(C.White,alpha); Gfx.Draw(image,-size/2,-size/2,size/image.Width,size/image.Height); Gfx.Pop();
            }
            else {Color(C.Gold,alpha);Gfx.Circle(false,810,342,84*pulse);Centered("UPGRADE",683,324,253,Ui.F32,C.Gold);}
            Centered(Lang.Upper(L(encounter.Name)),373,panelY+374,873,Ui.F32,C.Face);Gfx.SetFont(Ui.F16);Color(C.Muted,alpha);Gfx.Printf(L(encounter.Description),462,panelY+420,696,Align.Center);
            if(reveal.Elapsed>.65){Centered("CLICK OR PRESS ANY KEY TO CONTINUE",380,690,861,Ui.F16,C.Muted);Ui.Buttons.Add(new Button{X=0,Y=0,W=Ui.Width,H=Ui.Height,Label="DISMISS ENCOUNTER REVEAL",Action=()=>AppCore.DismissEncounterReveal()});}
        }
    }
}
