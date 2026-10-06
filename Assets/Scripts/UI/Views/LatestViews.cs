using System;
using static Tossup.UI.D;

namespace Tossup.UI
{
    public static class RunModifierView
    {
        public static void Draw(float x,float y,float size)
        {
            var g=Ui.Game;int i=0;
            if(g.RunEncounter is RunEncounterDef encounter)
            {
                float at=x+i++*(size+5);if(Ui.EncounterImages.TryGetValue(encounter.Id,out var image))ImageAt(image,at,y,size);else{Box(at,y,size,size,C.PanelDk);Centered("E",at,y+5,size,Ui.F20,C.Gold);}
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
            Centered(L("LEVEL %d CONTRACT", g.EncounterIndex), 70, 84, 1140, Ui.F32, C.Gold);
            Centered(L("Choose a challenge for a bonus, or skip it."), 70, 132, 1140, Ui.F20, C.Muted);
            string stageName = e.Endless.HasValue ? L("ENDLESS %d", e.Endless.Value) : L(e.Name);
            Centered(Lang.Upper(stageName), 70, 166, 1140, Ui.F16, C.Face);

            var options = e.ContractOptions ?? new System.Collections.Generic.List<string>();
            for (int i = 0; i < options.Count; i++)
            {
                string id = options[i];
                var d = Game.Contracts[id];
                float x = 160 + i * 330;
                Box(x, 220, 300, 360, C.PanelDk);
                Outline(x, 220, 300, 360, C.Line);
                Centered(L(d.Name), x + 16, 254, 268, Ui.F20, C.Gold);
                Centered(d.RewardText != null ? L(d.RewardText) : L("BONUS +%dG", d.Reward), x + 16, 304, 268,
                    d.RewardText != null ? Ui.F20 : Ui.F32, C.Green);
                Gfx.SetFont(Ui.F16);
                Color(C.Muted);
                Gfx.Printf(L(d.Description), x + 28, 358, 244, Align.Center);
                Centered(L("DRAWBACK"), x + 16, 438, 268, Ui.F16, C.Red);
                Color(C.Red);
                Gfx.Printf(L(d.Drawback), x + 28, 466, 244, Align.Center);
                Button(L("TAKE CONTRACT"), x + 24, 516, 252, 52, C.Blue, () => A.ChooseContract(id));
            }
            Button(L("SKIP CONTRACT"), 500, 638, 280, 54, C.PanelLight, A.SkipContract);
        }
    }

    public static class AugmentView
    {
        public static void Draw()
        {
            var g = Ui.Game;
            Frame(null);
            Centered(L("LEVEL %d AUGMENT", g.AugmentLevel ?? g.EncounterIndex), 70, 82, 1140, Ui.F32, C.Gold);
            if (g.AugmentPending != null) { DrawPending(g); return; }
            Centered(L("Choose a run upgrade or change a coin before this level."), 70, 128, 1140, Ui.F20, C.Muted);
            var options = g.AugmentOptions ?? new System.Collections.Generic.List<string>();
            const float width = 300, height = 390, gap = 30;
            float startX = (1280 - (width * 3 + gap * 2)) / 2;
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
            Centered(L(heading), 70, 126, 1140, Ui.F20, C.Gold);
            string subtitle = pending.RewardId != null
                ? L("NEW COIN: %s", Lang.CoinName(pending.RewardId))
                : L(def.Description);
            Centered(subtitle, 70, 158, 1140, Ui.F16, C.Muted);
            if (pending.RewardId != null)
            {
                CoinImage(pending.RewardId, 1154, 140, 42);
                CoinHover(pending.RewardId, 1154, 140, 42, 42);
            }

            var choices = Game.AugmentChoices(g);
            if (choices.Count == 0) return;
            int cols = choices.Count > 12 ? 4 : choices.Count > 6 ? 3 : 2;
            int rows = (choices.Count + cols - 1) / cols;
            const float gap = 16;
            float width = (float)Math.Floor((1160 - (cols - 1) * gap) / cols);
            float height = Math.Min(116, (float)Math.Floor((510 - (rows - 1) * gap) / rows));
            float startX = (1280 - (width * cols + gap * (cols - 1))) / 2;
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                float x = startX + (i % cols) * (width + gap);
                float y = 210 + (i / cols) * (height + gap);
                Rgba typeColor = C.Line;
                CoinType choiceType = default;
                bool typedChoice = pending.Id == "type_specialist" && Enum.TryParse(choice.Key, true, out choiceType);
                if (typedChoice) typeColor = CoinTypeColor(choiceType);
                Box(x, y, width, height, C.PanelDk);
                Outline(x, y, width, height, typeColor);
                bool hasCoin = choice.CoinId != null;
                if (hasCoin)
                {
                    CoinImage(choice.CoinId, x + 12, y + 10, 44);
                    CoinHover(choice.CoinId, x, y, width, height);
                }
                float textX = x + (hasCoin ? 64 : 16);
                float textWidth = width - (textX - x) - 12;
                string title = hasCoin ? Lang.CoinName(choice.CoinId) : L(choice.Title);
                Text(title, textX, y + 8, Ui.F20, typedChoice ? typeColor : C.Gold);
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
            if(reveal==null||game==null||game.RunEncounter==null)return;
            var encounter=game.RunEncounter;
            Ui.Buttons.Clear();
            float alpha=(float)Math.Max(0,Math.Min(1,reveal.Elapsed/.32));
            float rise=1-(float)Math.Pow(1-alpha,3);float pulse=1+(float)Math.Sin(reveal.Elapsed*5.5)*.025f;
            Color(C.Ink,.88f*alpha);Gfx.Rectangle(true,0,0,1280,800);
            float panelY=95+(1-rise)*84;Box(255,panelY,770,545,C.PanelDk);Outline(255,panelY,770,545,C.Gold);Outline(270,panelY+15,740,515,C.Line);
            Centered("RUN ENCOUNTER",295,panelY+34,690,Ui.F20,C.Gold);Centered("ONE RULE FOR THE WHOLE RUN",295,panelY+74,690,Ui.F16,C.Muted);
            if(Ui.EncounterImages.TryGetValue(encounter.Id,out var image)){float size=190*pulse*(.72f+.28f*rise);ImageAt(image,640-size/2,250+(1-rise)*40,size);}
            else {Color(C.Gold,alpha);Gfx.Circle(false,640,342,84*pulse);Centered("UPGRADE",540,324,200,Ui.F32,C.Gold);}
            Centered(encounter.Name.ToUpper(),295,panelY+374,690,Ui.F32,C.Face);Gfx.SetFont(Ui.F16);Color(C.Muted,alpha);Gfx.Printf(encounter.Description,365,panelY+420,550,Align.Center);
            if(reveal.Elapsed>.65){Centered("CLICK OR PRESS ANY KEY TO CONTINUE",300,690,680,Ui.F16,C.Muted);Ui.Buttons.Add(new Button{X=0,Y=0,W=1280,H=800,Label="DISMISS ENCOUNTER REVEAL",Action=()=>AppCore.DismissEncounterReveal()});}
        }
    }
}
