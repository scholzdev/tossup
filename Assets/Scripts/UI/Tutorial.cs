using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    sealed class TutorialStep
    {
        public string Title, Text, Hint;
        public float X, Y, W, H;
        public Func<GameState, bool> Wait;
    }

    public static class Tutorial
    {
        static TutorialStep S(string title, float x, float y, float w, float h, string text, Func<GameState,bool> wait = null, string hint = null) =>
            new TutorialStep { Title=title, X=x * Ui.Width / Ui.Width, Y=y, W=w * Ui.Width / Ui.Width, H=h, Text=text, Wait=wait, Hint=hint };

        static readonly List<TutorialStep> Steps = new List<TutorialStep>
        {
            S("THE GOAL",EncounterView.Left,EncounterView.BoardY,EncounterView.Width,EncounterView.BoardH,"Every fight lasts 5 rounds. Score more points than the enemy by the end of round 5. A tie goes to the House."),
            S("YOUR RESOURCES",880,EncounterView.BarY-4,EncounterView.Right-880,EncounterView.BarH+8,"Your gold and your energy. Strong coins cost energy to flip."),
            S("THE ENEMY",EncounterView.Left,EncounterView.EnemyY,EncounterView.Width,EncounterView.EnemyH,"The enemy flips coins from its own fixed pouch after every round. They stay hidden until you end the round. Hover its name to see the pouch."),
            S("YOUR HAND",EncounterView.Left,EncounterView.CardsY-6,EncounterView.Width,EncounterView.CardsH+12,"These coins are in your hand. Each card shows the coin's odds and what Heads and Tails do."),
            S("FLIP",EncounterView.Left,EncounterView.CardsY-6,EncounterView.Width,EncounterView.CardsH+12,"Click a coin to flip it. You may flip any number of them, in any order.",g=>g.LastResult!=null,"CLICK A COIN"),
            S("THE RESULT",EncounterView.Left,EncounterView.StripY-8,EncounterView.Width,EncounterView.CardsH+EncounterView.CardsY-EncounterView.StripY+14,"The card shows the side and points. The line above lists everything you flipped this round. Click the next coin when you are ready."),
            S("CHIPS",EncounterView.InnerX+320,EncounterView.FooterY-4,270,EncounterView.FooterH+8,"CHIPS are one-use helpers from the shop. Click one between flips."),
            S("FLIP MORE",EncounterView.Left,EncounterView.CardsY-6,EncounterView.Width,EncounterView.CardsH+12,"Flip two more coins. Two Heads in a row start a COMBO.",g=>g.Encounter!=null&&g.Encounter.Flips>=3,"FLIP TWO MORE"),
            S("COMBO POT",EncounterView.InnerRight-330,EncounterView.StripY-8,344,EncounterView.StatusY-EncounterView.StripY+36,"Repeated results raise the multiplier and build an unbanked pot. BANK locks it in as gold."),
            S("END THE ROUND",EncounterView.EndRoundX-6,EncounterView.EndRoundY-6,EncounterView.EndRoundW+12,EncounterView.EndRoundH+12,"Coins you did not flip stay in your hand. When you are done, end the round: the enemy flips and your hand refills.",g=>g.Encounter!=null&&g.Encounter.Round>=2,"PRESS END ROUND"),
            S("THE POUCH",EncounterView.InnerX-6,EncounterView.FooterY-2,270,30,"Flipped coins wait in the discard pile. When the pouch runs out they are shuffled back in."),
            S("THAT'S IT",330,280,620,200,"Win eight fights, ending with The House. Between fights you buy coins, chips and prizes."),
        };

        public static int Count => Steps.Count;
        public static int StepIndex => Ui.Tutorial?.Step ?? 0;
        public static bool Interactive => Ui.Tutorial != null && Steps[Ui.Tutorial.Step-1].Wait != null;

        public static void Start()
        {
            var game = Game.New(7, "blade", new List<string>(), new List<CoinDef>{CoinCatalog.Normal}, 1, false);
            game.SetRunEncounter(null);
            game.Tutorial = true;
            game.TutorialHeads = 3;
            game.Items = new List<string>{"energy_drink"};
            game.Encounter.EnemyDraw = 1; // a gentle first enemy
            game.Paused = false;
            Ui.Game = game;
            Ui.Tutorial = new TutorialState();
            Ui.EncounterReveal = null;
            Ui.FlipAnimation = null;
            Ui.ResolveTimer = 0;
        }

        public static void Finish()
        {
            Ui.Tutorial = null;
            Ui.Game = null;
            Ui.FlipAnimation = null;
            Ui.Screen = UiScreen.Title;
        }

        public static void Next()
        {
            if (Ui.Tutorial == null) return;
            Ui.Tutorial.Step++;
            if (Ui.Tutorial.Step > Steps.Count) Finish();
        }

        public static void Update()
        {
            if (Ui.Tutorial == null || Ui.Game == null) return;
            var step = Steps[Ui.Tutorial.Step-1];
            if (step.Wait != null && step.Wait(Ui.Game)) Next();
        }

        public static void Draw()
        {
            if (Ui.Tutorial == null) return;
            var step = Steps[Ui.Tutorial.Step-1];
            float spotHeight = step.H;
            Color(C.Ink,.72f);
            Gfx.Rectangle(true,0,0,Ui.Width,step.Y);
            Gfx.Rectangle(true,0,step.Y+spotHeight,Ui.Width,800-step.Y-spotHeight);
            Gfx.Rectangle(true,0,step.Y,step.X,spotHeight);
            Gfx.Rectangle(true,step.X+step.W,step.Y,Ui.Width-step.X-step.W,spotHeight);
            Color(C.Orange);Gfx.SetLineWidth(3);Gfx.Rectangle(false,step.X-3,step.Y-3,step.W+6,spotHeight+6,6);Gfx.SetLineWidth(1);

            float width=480;var lines=Ui.F16.GetWrap(L(step.Text),width-40);float height=96+lines.Count*20;
            float x=Math.Min(Ui.Width-width-20,Math.Max(20,step.X+step.W/2-width/2));float y=step.Y+spotHeight+20;
            if(y+height>790)y=step.Y-height-20;if(y<10)y=Math.Max(10,step.Y+16);
            Box(x,y,width,height,C.PanelDk);Outline(x,y,width,height,C.Orange);
            Text(step.Title,x+20,y+14,Ui.F20,C.Orange);Gfx.SetFont(Ui.F16);Color(C.Face);Gfx.Printf(L(step.Text),x+20,y+46,width-40);
            Text(step.Wait!=null?(step.Hint??"CONTINUE"):"CLICK ANYWHERE TO CONTINUE",x+20,y+height-28,Ui.F16,C.Gold);
            string count=Ui.Tutorial.Step+" / "+Steps.Count;Text(count,x+width-20-Ui.F16.GetWidth(count),y+height-28,Ui.F16,C.Muted);

            var kept=new List<Button>();
            if(step.Wait!=null)
                foreach(var b in Ui.Buttons){float cx=b.X+b.W/2,cy=b.Y+b.H/2;if(cx>=step.X&&cx<=step.X+step.W&&cy>=step.Y&&cy<=step.Y+spotHeight)kept.Add(b);}
            else kept.Add(new Button{X=0,Y=0,W=Ui.Width,H=800,Action=Next});
            Ui.Buttons=kept;
            Button("SKIP",Ui.Width-160,750,140,36,C.PanelLight,Finish);
        }
    }

}
