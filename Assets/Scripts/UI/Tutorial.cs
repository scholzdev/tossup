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
            S("THE GOAL",330,40,580,120,"Each level has a QUOTA: points you must score before your coins run out. The bar fills as you score."),
            S("YOUR RESOURCES",930,96,290,46,"Coins left in your stack, your gold, and your energy. Strong coins cost energy to flip."),
            S("THE COIN BANK",70,170,240,480,"Your remaining coins stay visible in the bank. Click any coin to choose what to play next."),
            S("THE COIN IN PLAY",330,170,880,480,"The coin shows its Heads and Tails effects and its odds. Read them before you flip."),
            S("FLIP",550,676,260,64,"Press FLIP or Space to toss the coin.",g=>g.LastResult!=null,"PRESS FLIP"),
            S("THE RESULT",330,450,880,300,"The banner shows the side and points. Press NEXT COIN to continue.",g=>g.LastResult!=null&&!Ui.Holding,"PRESS NEXT COIN"),
            S("CHIPS",330,650,280,110,"CHIPS are one-use helpers from the shop. Click one while a coin is ready to flip."),
            S("FLIP AGAIN",550,676,260,64,"Flip this one too. Two Heads in a row start a COMBO.",g=>g.Encounter!=null&&g.Encounter.Cleared,"PRESS FLIP"),
            S("COMBO POT",990,176,205,145,"Repeated results raise the multiplier and build an unbanked pot. BANK locks it in."),
            S("PUSH",510,676,260,92,"PUSH flips the next coin to build your combo. A broken streak loses the unbanked pot."),
            S("OPEN THE SHOP",930,54,170,38,"Keep flipping for extra gold, or open the shop.",g=>g.Phase==Phase.Shop,"PRESS OPEN SHOP"),
            S("THE SHOP",70,160,920,440,"Between levels you buy coins, chips, a prize, and reroll the offers."),
            S("DECK TOOL",1000,238,220,220,"Select a coin below to remove it from the deck for gold."),
            S("YOUR DECK",70,600,920,150,"Your coins are shown here. Dark slots on the right are extra deck slots."),
            S("NEXT ROUND",1000,586,220,170,"Press the red button when you are ready for the next level.",g=>g.Phase==Phase.Encounter,"PRESS NEXT ROUND"),
            S("A NEW MODIFIER",330,540,300,110,"From level 2 on, every level has a modifier. Read it before you flip."),
            S("RUNNING OUT OF COINS",930,96,100,46,"If the stack empties, bank the combo or buy an exchange to return played coins."),
            S("THAT'S IT",330,280,620,200,"Beat eight levels ending with The House. Encounters and Augments shape the run."),
        };

        public static int Count => Steps.Count;
        public static int StepIndex => Ui.Tutorial?.Step ?? 0;
        public static bool Interactive => Ui.Tutorial != null && Steps[Ui.Tutorial.Step-1].Wait != null;

        public static void Start()
        {
            var game = Game.New(7, "blade", new List<string>(), new List<CoinDef>{CoinCatalog.Normal,CoinCatalog.Normal,CoinCatalog.Normal}, true, 1, false);
            game.SetRunEncounter(null);
            game.ContractsEnabled = false;
            game.Tutorial = true;
            game.TutorialHeads = 3;
            game.Items = new List<string>{"energy_drink"};
            game.Encounter.Quota = game.Encounter.MaxQuota = 2;
            game.Encounter.Payout = Game.Stage(1).Payout;
            Game.MulliganDone(game);
            game.Paused = false;
            Ui.Game = game;
            Ui.Tutorial = new TutorialState();
            Ui.EncounterReveal = null;
            Ui.FlipAnimation = null;
            Ui.Holding = false;
            Ui.ResolveTimer = 0;
        }

        public static void Finish()
        {
            Ui.Tutorial = null;
            Ui.Game = null;
            Ui.FlipAnimation = null;
            Ui.Holding = false;
            Ui.Screen = "title";
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
            float spotHeight = step.Title == "THE COIN BANK" ? EncounterView.BankPanelHeight : step.H;
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
