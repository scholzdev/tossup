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
            var g=Ui.Game; Frame(null); Centered("LEVEL "+g.EncounterIndex+" CONTRACT",70,82,1140,Ui.F32,C.Gold);
            Centered("Choose a challenge for a bonus, or skip it.",70,132,1140,Ui.F20,C.Muted);
            var options=g.Encounter.ContractOptions??new System.Collections.Generic.List<string>();
            for(int i=0;i<options.Count;i++){
                string id=options[i];var d=Game.Contracts[id];float x=155+i*330;
                Box(x,220,300,360,C.PanelDk);Outline(x,220,300,360,C.Line);Centered(d.Name,x+16,250,268,Ui.F20,C.Gold);
                Centered(d.RewardText??("BONUS +"+d.Reward+"G"),x+16,305,268,Ui.F32,C.Green);
                Gfx.SetFont(Ui.F16);Color(C.Face);Gfx.Printf(d.Description,x+28,365,244,Align.Center);
                Centered("DRAWBACK",x+16,440,268,Ui.F16,C.Red);Gfx.SetFont(Ui.F16);Color(C.Red);Gfx.Printf(d.Drawback,x+28,468,244,Align.Center);
                Button("TAKE CONTRACT",x+24,520,252,44,C.Blue,()=>A.ChooseContract(id));
            }
            Button("SKIP CONTRACT",500,638,280,54,C.PanelLight,A.SkipContract);
        }
    }

    public static class AugmentView
    {
        public static void Draw()
        {
            var g=Ui.Game;Frame(null);Centered("LEVEL "+g.AugmentLevel+" AUGMENT",70,82,1140,Ui.F32,C.Gold);
            if(g.AugmentPending!=null){DrawPending(g);return;}
            Centered("Choose a run upgrade before this level.",70,128,1140,Ui.F20,C.Muted);
            var options=g.AugmentOptions??new System.Collections.Generic.List<string>();
            for(int i=0;i<options.Count;i++){string id=options[i];var d=Game.AugmentDefs[id];float x=155+i*330;
                Box(x,205,300,390,C.PanelDk);Outline(x,205,300,390,C.Line);Centered(d.Name,x+16,230,268,Ui.F20,C.Gold);
                if(Ui.AugmentImages.TryGetValue(id,out var image))ImageAt(image,x+102,275,96);
                Centered(d.Tier.ToUpper()+" TIER",x+16,390,268,Ui.F16,C.Muted);Gfx.SetFont(Ui.F16);Color(C.Face);Gfx.Printf(d.Description,x+28,430,244,Align.Center);
                Button("CHOOSE AUGMENT",x+24,530,252,48,C.Blue,()=>A.ChooseAugment(id));}
        }
        static void DrawPending(GameState g)
        {
            Centered("CHOOSE AN OPTION",70,126,1140,Ui.F20,C.Gold);var choices=Game.AugmentChoices(g);int cols=choices.Count>6?3:2;float width=360,gap=18,start=(1280-(width*cols+gap*(cols-1)))/2;
            for(int i=0;i<choices.Count;i++){var c=choices[i];float x=start+(i%cols)*(width+gap),y=190+(i/cols)*105;Box(x,y,width,92,C.PanelDk);Outline(x,y,width,92,C.Line);if(c.CoinId!=null){CoinImage(c.CoinId,x+10,y+10,48);Text(Content.Coins[c.CoinId].Name,x+68,y+10,Ui.F20,C.Gold);}else Text(c.Title,x+16,y+10,Ui.F20,C.Gold);Text(c.UpgradeName??c.Detail,x+68,y+40,Ui.F16,C.Face);Button("CHOOSE",x+width-100,y+50,88,30,C.Blue,()=>A.ChooseAugmentOption(c.Key));}
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
            Color(C.Ink,.88f*alpha);Gfx.Rectangle(true,0,0,1280,800);
            float panelY=95+(1-rise)*84;Box(255,panelY,770,545,C.PanelDk);Outline(255,panelY,770,545,C.Gold);Outline(270,panelY+15,740,515,C.Line);
            Centered("RUN ENCOUNTER",295,panelY+34,690,Ui.F20,C.Gold);Centered("ONE RULE FOR THE WHOLE RUN",295,panelY+74,690,Ui.F16,C.Muted);
            if(Ui.EncounterImages.TryGetValue(game.RunEncounterId,out var image)){float size=190*pulse*(.72f+.28f*rise);ImageAt(image,640-size/2,250+(1-rise)*40,size);}
            else {Color(C.Gold,alpha);Gfx.Circle(false,640,342,84*pulse);Centered("UPGRADE",540,324,200,Ui.F32,C.Gold);}
            Centered(encounter.Name.ToUpper(),295,panelY+374,690,Ui.F32,C.Face);Gfx.SetFont(Ui.F16);Color(C.Muted,alpha);Gfx.Printf(encounter.Description,365,panelY+420,550,Align.Center);
            if(reveal.Elapsed>.65){Centered("CLICK OR PRESS ANY KEY TO CONTINUE",300,690,680,Ui.F16,C.Muted);Ui.Buttons.Add(new Button{X=0,Y=0,W=1280,H=800,Label="DISMISS ENCOUNTER REVEAL",Action=()=>AppCore.DismissEncounterReveal()});}
        }
    }
}
