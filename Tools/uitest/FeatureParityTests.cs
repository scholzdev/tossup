using System;
using System.Collections.Generic;
using Tossup;
using Tossup.UI;

static class FeatureParityTests
{
    static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException("feature parity: " + message);
    }

    public static void Run()
    {
        SavedRunRoundTrip();
        TutorialFlow();
        RuntimeModesAndSandbox();
        TitleAndEncounterScreens();
        DiscardParity();
        EncounterRevealFlow();
        int seeds=1000;
        if(int.TryParse(Environment.GetEnvironmentVariable("TOSSUP_SEED_SWEEP"),out var requested))seeds=Math.Max(1,requested);
        SeedSweep(1, seeds);
        Console.WriteLine("features: saves, tutorial, modes, reveal and "+seeds+" full-run seed sweep passed");
    }

    static void DiscardParity()
    {
        RuntimeMode.Configure(false, false);
        var opening = Game.New(77101, "trader", null, new List<string> { "loaded", "normal", "normal" }, true);
        int fuseUid = opening.Mulligan.Hand[0];
        var fuse = Game.GetCoin(opening, fuseUid);
        fuse.Id = "fuse";
        Check(Game.MulliganDiscard(opening, new List<int> { fuseUid }) == 1, "opening-hand discard removes a marked coin");
        Check(fuse.Charge == 6, "opening-hand discard fires the coin discard hook and growth");

        for (int i = 0; i < 2; i++)
        {
            int uid = opening.NextUid++;
            opening.Coins.Add(new CoinInst { Uid = uid, Id = "normal" });
            opening.Encounter.Pile.Add(uid);
        }
        Game.MulliganDone(opening);
        Ui.Game = opening;
        Ui.FlipAnimation = null;
        Ui.Holding = false;
        opening.Encounter.BankDiscards = 1;
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "DISCARD MODE: OFF"), "Crystal Ball exposes an explicit discard-mode toggle");
        Check(Ui.Buttons.Exists(b => b.Label == "BANK COIN"), "bank coins remain selectable while a discard is armed");
        var toggle = Ui.Buttons.Find(b => b.Label == "DISCARD MODE: OFF");
        toggle.Action();
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "DISCARD MODE: ON"), "discard mode can be enabled");
        Check(Ui.Buttons.Exists(b => b.Label == "BANK DISCARD"), "discard mode exposes a bank coin action");
        var discard = Ui.Buttons.Find(b => b.Label == "BANK DISCARD" && Math.Abs(b.Y - 270) < .01f);
        int discardUid = opening.Encounter.Queue[1];
        discard.Action();
        Check(opening.Encounter.BankDiscards == 0 && opening.Encounter.Discards == 2,
            "bank discard spends the Crystal Ball charge (charges=" + opening.Encounter.BankDiscards + ", discards=" + opening.Encounter.Discards + ", phase=" + opening.Phase + ", target=" + discardUid + ")");
        Check(opening.Encounter.Queue.Count == Game.Visible, "discarding a non-front coin refills the visible bank");
        Check(opening.Dealt != null && opening.Dealt.Uid == opening.Encounter.Queue[0] && !opening.Encounter.Queue.Contains(discardUid),
            "discarding a later bank coin keeps the dealt coin active");
        Check(!Ui.BankDiscardMode, "bank discard exits discard mode");
        Ui.Game = null;
    }

    static void SavedRunRoundTrip()
    {
        var game = Game.New(4401, "trader", new List<string> { "loaded" }, new List<string> { "loaded", "normal" }, true, 3);
        Game.MulliganDone(game);
        game.Player.Gold = 37;
        game.Coins[0].Upgrade = "safer_bet";
        game.Augments.Add("bankers_cut");
        game.AugmentData["type_specialist"] = "fortune";
        game.Encounter.ComboPot = 6;
        string json = RunSave.Encode(game);
        var restored = RunSave.Decode(json);
        Check(restored != null, "valid saved run restores");
        Check(restored.Seed == game.Seed && restored.RngState == game.RngState, "RNG state survives restoration");
        Check(restored.CharacterId == "trader" && restored.Stake == 3 && restored.Player.Gold == 37, "run identity survives restoration");
        Check(restored.Coins.Count == 2 && restored.Coins[0].Upgrade == "safer_bet", "owned coin state survives restoration");
        Check(restored.Augments.Contains("bankers_cut") && restored.AugmentData["type_specialist"] == "fortune", "augment state survives restoration");
        Check(restored.Encounter.ComboPot == 6 && restored.Mulligan == null && restored.Dealt != null, "safe-point encounter state survives restoration");
        Check(RunSave.Decode("{\"version\":999}") == null && RunSave.Decode("broken") == null, "damaged or incompatible saves are rejected");
        var unsafeGame=Game.New(4402,"blade",null,null,true);
        bool rejected=false;try{RunSave.Encode(unsafeGame);}catch(InvalidOperationException){rejected=true;}
        Check(rejected,"mid-level progress is never written as a resume point");

        Ui.Game = game;
        A.SaveRun();
        Ui.Game = null;
        Check(A.HasSavedRun() && A.LoadRun(), "saved run can be continued through the UI action layer");
        Check(Ui.Game != null && Ui.Game.Seed == 4401, "continued run is installed in UI state");
        A.DeleteRun();
        Check(!A.HasSavedRun(), "saved run is deleted explicitly");
        Ui.Game = null;
    }

    static void TutorialFlow()
    {
        Tutorial.Start();
        Check(Ui.Game != null && Ui.Game.Tutorial && Ui.Tutorial != null, "tutorial starts a throwaway deterministic run");
        Check(Ui.Game.Seed == 7 && Ui.Game.Coins.Count == 3 && Ui.Game.Items.Contains("energy_drink"), "tutorial scene is deterministic");
        Check(Tutorial.Count == 18 && Tutorial.StepIndex == 1, "tutorial exposes every guided step");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "SKIP"), "tutorial overlay draws a skip control");
        Tutorial.Next();
        Check(Tutorial.StepIndex == 2, "tutorial advances manually");
        Tutorial.Finish();
        Check(Ui.Game == null && Ui.Tutorial == null && Ui.Screen == "title", "tutorial exits without retaining its run");
    }

    static void RuntimeModesAndSandbox()
    {
        RuntimeMode.Configure(true, false);
        var dev = Game.New(5501, "blade", null, null, false);
        Check(RuntimeMode.Dev && dev.Player.Gold == 5000, "developer mode grants test gold");
        var profile = Profile.New();
        RuntimeMode.ApplyProfile(profile);
        foreach (var id in Content.CharacterOrder)
            Check(Profile.CharacterUnlocked(profile, id), "developer mode unlocks character " + id);

        const string scene = "{\"coins\":[\"loaded\",\"jackpot\"],\"character\":\"trader\",\"stake\":4,\"gold\":91,\"energy\":7,\"odds\":{\"loaded\":{\"heads\":0.2,\"tie\":0.3}}}";
        var sandbox = Game.NewSandbox(SandboxConfig.Decode(scene));
        Check(sandbox.Sandbox != null && sandbox.CharacterId == "trader" && sandbox.Stake == 4, "sandbox JSON selects character and stake");
        Check(sandbox.Player.Gold == 91 && sandbox.Player.Energy == 7 && sandbox.Coins.Count == 2, "sandbox JSON selects resources and deck");
        Check(Math.Abs(Game.Probability(sandbox, sandbox.Coins[0]) - .2) < 1e-9 && Math.Abs(Game.TieProbability(sandbox, sandbox.Coins[0]) - .3) < 1e-9, "sandbox odds override runtime odds");
        RuntimeMode.Configure(false, false);
    }

    static void EncounterRevealFlow()
    {
        var game = Game.New(6601, "blade", null, null, false);
        Ui.Game = game;
        Ui.EncounterReveal = new EncounterReveal { Elapsed = 0 };
        AppCore.Draw();
        Check(Ui.Buttons.Count == 0, "encounter reveal blocks game controls while animating");
        AppCore.Update(.7);
        AppCore.Draw();
        Check(Ui.Buttons.Count == 1 && Ui.Buttons[0].Label == "DISMISS ENCOUNTER REVEAL", "reveal adds dismissal after its opening animation");
        Check(AppCore.DismissEncounterReveal() && Ui.EncounterReveal == null, "reveal closes after its minimum display time");
        Ui.Game = null;
    }

    static void TitleAndEncounterScreens()
    {
        RuntimeMode.Configure(false, false);
        Ui.Game = null;
        Ui.Tutorial = null;
        Ui.EncounterReveal = null;
        Ui.Screen = "title";
        Check(Ui.UiImages.ContainsKey("title_scene"), "the original title scene with its three coins is loaded");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "PLAY" && Math.Abs(b.X - 100) < .01f && Math.Abs(b.Y - 240) < .01f),
            "title actions use the original left-side menu layout");

        Ui.SelectedCharacter = "blade";
        A.Start(6602);
        Ui.EncounterReveal = null; // inspect the title after its separate run-encounter reveal
        A.OpenMenu();
        AppCore.Draw();
        var newRunButton = Ui.Buttons.Find(b => b.Label == "NEW RUN");
        Check(newRunButton != null, "an active run exposes New Run from the title (screen=" + Ui.Screen + ", paused=" + Ui.Game?.Paused +
            ", buttons=" + string.Join(",", Ui.Buttons.ConvertAll(b => b.Label)) + ")");
        newRunButton.Action();
        Check(Ui.Confirm != null && Ui.Confirm.Title == "NEW RUN", "replacing an active run requires the original confirmation");
        Ui.Confirm = null;
        Ui.Game.Paused = false;
        A.Update(.1);
        Check(Ui.Game.Mulligan != null, "the opening-hand encounter screen remains interactive");
        Check(!A.HasSavedRun(), "the opening hand is not mistaken for a resumable level start");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label.StartsWith("START LEVEL")), "the opening-hand screen exposes Start Level");
        A.NextOrFlip();
        Check(Ui.Game.Phase == Phase.Contract, "starting a normal level offers its contract screen");
        A.Update(.1);
        Check(A.HasSavedRun() && A.LoadRun() && Ui.Game.Phase == Phase.Contract && Ui.Game.Mulligan != null,
            "continuing a saved contract restores its encounter screen and opening hand");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label.StartsWith("TAKE CONTRACT")) && Ui.Buttons.Exists(b => b.Label == "SKIP CONTRACT"),
            "the contract screen exposes its original choices");
        A.SkipContract();
        Check(Ui.Game.Phase == Phase.Encounter && Ui.Game.Mulligan == null, "skipping a contract starts the encounter");
        Ui.Game = null;
        Ui.EncounterReveal = null;
    }

    static void SeedSweep(int first, int count)
    {
        string[] characters = { "blade", "trader", "seer" };
        for (int seed = first; seed < first + count; seed++)
        {
            var game = Game.New(seed, characters[seed % characters.Length], null, null, false, 1 + seed % Game.Stakes.Count);
            int guard = 0;
            while (game.Phase != Phase.GameOver && game.Phase != Phase.Victory && guard++ < 2000)
            {
                if (game.Phase == Phase.Encounter)
                {
                    if (game.Mulligan != null) Game.MulliganDone(game);
                    else if (game.Pending != null) Game.Resolve(game);
                    else if (game.Encounter.Cleared) Game.EndLevel(game);
                    else if (game.Dealt != null && Game.CanFlip(game)) Game.Flip(game);
                    else if (game.Dealt != null) Game.Discard(game);
                    else if (Game.CanExchange(game)) Game.Exchange(game);
                    else Game.GiveUp(game);
                }
                else if (game.Phase == Phase.Shop)
                {
                    if (game.Coins.Count >= game.Slots && game.Slots < Game.DeckMax && game.Player.Gold >= Game.SlotPrice(game)) Game.BuySlot(game);
                    for (int i = 0; i < game.ShopOffers.Count; i++) Game.Buy(game, i);
                    if (game.ShopRelic != null) Game.BuyRelic(game);
                    for (int i = 0; i < game.ShopItems.Count; i++) Game.BuyItem(game, i);
                    Game.LeaveShop(game);
                }
                else if (game.Phase == Phase.Augment)
                {
                    Check(game.AugmentOptions != null && game.AugmentOptions.Count > 0, "augment offers exist for seed " + seed);
                    Check(Game.ChooseAugment(game, game.AugmentOptions[0]), "augment selection works for seed " + seed);
                    if (game.AugmentPending != null)
                    {
                        var choices = Game.AugmentChoices(game);
                        Check(choices.Count > 0 && Game.ChooseAugmentOption(game, choices[0].Key), "augment option works for seed " + seed);
                    }
                }
                else if (game.Phase == Phase.Contract) Game.SkipContract(game);
                Check(double.IsFinite(game.Player.Gold) && double.IsFinite(game.Player.Energy), "finite resources for seed " + seed);
                Check(game.Coins.Count > 0 && game.Coins.Count <= Game.DeckMax, "valid deck size for seed " + seed);
                if (game.Encounter != null) Check(double.IsFinite(game.Encounter.Quota) && game.Encounter.Queue.Count + game.Encounter.Pile.Count <= game.Coins.Count, "valid encounter state for seed " + seed);
            }
            Check(guard < 2000, "seed terminates without a rules loop: " + seed + " phase="+game.Phase+" level="+game.EncounterIndex+
                " clear="+game.Encounter?.Cleared+" dealt="+(game.Dealt!=null)+" pending="+(game.Pending!=null)+" left="+(game.Encounter==null?-1:Game.CoinsLeft(game))+
                " exchanges="+game.Encounter?.Exchanges+" canExchange="+(game.Encounter!=null&&Game.CanExchange(game)));
        }
    }
}
