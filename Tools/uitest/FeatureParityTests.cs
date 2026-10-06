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
        CanSelectNextCoinWhileHolding();
        ImpossibleOddsHideUnavailableSide();
        CollectionCatalogCategories();
        CoinTypeColorPalette();
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
        var opening = Game.New(77101, "trader", null, new List<CoinDef> { CoinCatalog.Loaded, CoinCatalog.Normal, CoinCatalog.Normal }, true);
        int fuseUid = opening.Mulligan.Hand[0];
        var fuse = Game.GetCoin(opening, fuseUid);
        fuse.Definition = CoinCatalog.Fuse;
        Check(Game.MulliganDiscard(opening, new List<int> { fuseUid }) == 1, "opening-hand discard removes a marked coin");
        Check(fuse.Charge == 6, "opening-hand discard fires the coin discard hook and growth");

        for (int i = 0; i < 2; i++)
        {
            int uid = opening.NextUid++;
            opening.Coins.Add(new CoinInst { Uid = uid, Definition = CoinCatalog.Normal });
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
        var discardTargets = Ui.Buttons.FindAll(b => b.Label == "BANK DISCARD");
        Check(discardTargets.Count > 1, "discard mode includes a non-front bank coin");
        var discard = discardTargets[1];
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
        var game = Game.New(4401, "trader", new List<string> { "loaded" }, new List<CoinDef> { CoinCatalog.Loaded, CoinCatalog.Normal }, true, 3);
        game.Player.Gold = 37;
        game.Coins[0].Upgrade = UpgradeCatalog.SaferBet;
        game.Augments.Add("bankers_cut");
        game.Augments.Add("type_specialist");
        game.AugmentData["type_specialist"] = "fortune";
        game.Encounter.ComboPot = 6;
        string json = RunSave.Encode(game);
        var restored = RunSave.Decode(json);
        Check(restored != null, "valid saved run restores");
        Check(restored.Seed == game.Seed && restored.RngState == game.RngState, "RNG state survives restoration");
        Check(restored.CharacterId == "trader" && restored.Stake == 3 && restored.Player.Gold == 37, "run identity survives restoration");
        Check(restored.Coins.Count == 2 && restored.Coins[0].Upgrade == UpgradeCatalog.SaferBet, "owned coin state survives restoration");
        Check(restored.Augments.Contains("bankers_cut") && restored.AugmentData["type_specialist"] == "fortune", "augment state survives restoration");
        Check(restored.Encounter.ComboPot == 6 && restored.Mulligan != null && restored.Dealt == null, "safe-point encounter state survives restoration");
        Check(RunSave.Decode("{\"version\":999}") == null && RunSave.Decode("broken") == null, "damaged or incompatible saves are rejected");
        var unsafeGame=Game.New(4402,"blade",null,null,false);
        Game.Flip(unsafeGame);
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
        var clockGame = new GameState { Phase = Phase.Encounter, Encounter = new Encounter() };
        Game.AdvanceClock(clockGame, 1.5);
        Check(Math.Abs(clockGame.Encounter.ElapsedSeconds - 1.5) < 1e-9, "active encounter time advances");
        clockGame.Paused = true;
        Game.AdvanceClock(clockGame, 2);
        Check(Math.Abs(clockGame.Encounter.ElapsedSeconds - 1.5) < 1e-9, "paused encounter time stops");
        clockGame.Paused = false;
        clockGame.Mulligan = new Mulligan();
        Game.AdvanceClock(clockGame, 2);
        Check(Math.Abs(clockGame.Encounter.ElapsedSeconds - 1.5) < 1e-9, "the encounter clock waits until mulligan ends");
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
        var odds = Game.GetOdds(sandbox, sandbox.Coins[0]);
        Check(Math.Abs(odds.Heads - .2) < 1e-9 && Math.Abs(odds.Edge - .3) < 1e-9 && Math.Abs(odds.Tails - .5) < 1e-9,
            "coin odds expose Heads, Edge, and Tails as one distribution");
        var adjustedOdds = new Odds(.35, .1, .55);
        adjustedOdds.Heads += .04;
        adjustedOdds.Normalize();
        Check(Math.Abs(adjustedOdds.Heads - .39) < 1e-9 && Math.Abs(adjustedOdds.Edge - .1) < 1e-9 && Math.Abs(adjustedOdds.Tails - .51) < 1e-9,
            "changing one outcome transfers its probability from Tails");

        var hookCoin = new CoinInst { Uid = 1, Definition = CoinCatalog.Normal };
        var hookGame = new GameState
        {
            Phase = Phase.Encounter,
            Encounter = new Encounter(),
            Coins = new List<CoinInst> { hookCoin },
        };
        double tickElapsed = -1;
        int gameOutcomeCalls = 0;
        Action<HookContext> tickHook = ctx => tickElapsed = ctx.ElapsedSeconds;
        Action<HookContext, Odds> oddsHook = (ctx, value) => value.Heads += .1;
        Action<HookContext, GameEvent> gameOutcomeHook = (ctx, evt) =>
        {
            if (ctx.Coin == hookCoin && evt.Result == "Heads") gameOutcomeCalls++;
        };
        CoinCatalog.Normal.On.Time.Tick += tickHook;
        CoinCatalog.Normal.On.Coins.Odds += oddsHook;
        CoinCatalog.Normal.On.Game.Coins.Outcome += gameOutcomeHook;
        try
        {
            Game.AdvanceClock(hookGame, 1.25);
            Check(Math.Abs(tickElapsed - 1.25) < 1e-9, "On.Time.Tick receives the elapsed encounter time");
            Check(Math.Abs(Game.GetOdds(hookGame, hookCoin).Heads - .75) < 1e-9, "On.Coins.Odds can change a coin's chance");
            Signal.Emit(GameSignal.CoinOutcome, new GameEvent { Game = hookGame, Inst = hookCoin, Result = "Heads" });
            Check(gameOutcomeCalls == 1, "On.Game.Coins.Outcome receives run-wide coin events");

            var shop = Game.NewSandbox(new SandboxConfig { Coins = new List<string> { "normal" }, Gold = 9, Seed = 3 });
            Game.OpenSandboxShop(shop);
            shop.ShopItems[0] = "force_heads";
            Action<HookContext, ShopPurchase> discount = (ctx, purchase) =>
            {
                if (purchase.Kind == ShopPurchaseKind.Item) purchase.Cost -= 4;
            };
            CoinCatalog.Normal.On.Game.Shop.BeforePurchase += discount;
            try { Check(Game.BuyItem(shop, 0) && shop.Player.Gold == 1, "On.Game.Shop.BeforePurchase can adjust purchase cost"); }
            finally { CoinCatalog.Normal.On.Game.Shop.BeforePurchase -= discount; }
        }
        finally
        {
            CoinCatalog.Normal.On.Time.Tick -= tickHook;
            CoinCatalog.Normal.On.Coins.Odds -= oddsHook;
            CoinCatalog.Normal.On.Game.Coins.Outcome -= gameOutcomeHook;
        }
        RuntimeMode.Configure(false, false);
    }

    static void EncounterRevealFlow()
    {
        foreach(bool dev in new[] {false,true})
        {
            RuntimeMode.Configure(dev,false);
            Ui.SelectedCharacter="blade";Ui.Tutorial=null;Ui.Confirm=null;
            A.Start(6601);
            Check(Ui.Game.RunEncounterId!=null && Ui.EncounterReveal!=null, "normal and developer Start show the assigned encounter");
            AppCore.Draw();
            Check(Ui.Buttons.Count == 0, "encounter reveal blocks game controls while animating");
            AppCore.Update(.2);
            Check(Math.Abs(Ui.EncounterReveal.Elapsed-.2)<1e-9 && Ui.Game.Encounter.Flips==0, "encounter reveal animates before gameplay");
            AppCore.Update(.5);
            AppCore.Draw();
            Check(Ui.Buttons.Count == 1 && Ui.Buttons[0].Label == "DISMISS ENCOUNTER REVEAL", "reveal adds dismissal after its opening animation");
            Check(AppCore.DismissEncounterReveal() && Ui.EncounterReveal == null, "reveal closes after its minimum display time");
        }
        RuntimeMode.Configure(false,false);
        Ui.Game = null;
    }

    static void CanSelectNextCoinWhileHolding()
    {
        RuntimeMode.Configure(false, false);
        var game = Game.New(6610, "blade", null, null, false);
        var last = game.Dealt;
        Check(last != null, "selection test starts with a dealt coin");
        game.Dealt = null;
        game.Encounter.Queue.Remove(last.Uid);
        game.Encounter.Played.Add(last.Uid);
        game.LastResult = new FlipState { Uid = last.Uid, CoinId = Game.GetCoin(game, last.Uid).Id, Result = Side.Heads };
        Ui.Game = game;
        Ui.Holding = true;
        Ui.FlipAnimation = null;
        Ui.EncounterReveal = null;
        AppCore.Draw();
        var select = Ui.Buttons.Find(b => b.Label == "BANK COIN");
        Check(select != null, "the bank stays selectable while the last result is held");
        select.Action();
        Check(game.Dealt != null && game.Dealt.Uid != last.Uid, "selecting a bank coin deals it for the next flip");
        Ui.Holding = false;
        Ui.Game = null;
        Hooks.Unbind();
    }

    static void ImpossibleOddsHideUnavailableSide()
    {
        RuntimeMode.Configure(false, false);
        Action<HookContext, Odds> forceHeads = (ctx, odds) => { odds.Heads = 1; odds.Edge = 0; };
        CoinCatalog.Normal.On.Coins.Odds += forceHeads;
        try
        {
            var game = Game.NewSandbox(new SandboxConfig { Coins = new List<string> { "normal", "safeport" }, Seed = 6611 });
            var last = game.Dealt;
            var lastCoin = Game.GetCoin(game, last.Uid);
            game.Coins.Remove(lastCoin);
            game.Encounter.Queue.Remove(last.Uid);
            game.Encounter.Played.Add(last.Uid);
            game.Dealt = null;
            game.LastResult = new FlipState
            {
                Uid = last.Uid, CoinId = lastCoin.Id, Probability = 1, Result = Side.Heads, Gained = 2,
            };
            Ui.Game = game;
            Ui.Holding = true;
            Ui.FlipAnimation = null;
            var backend = (HeadlessBackend)Gfx.Backend;
            backend.Capture = true;
            backend.Texts.Clear();
            backend.Images.Clear();
            AppCore.Draw();
            Check(backend.Images.ContainsKey("coins/normal"), "last-result coin art remains visible after the coin leaves play");
            Check(backend.Texts.Exists(t => t.Value == "HEADS"), "possible outcome details remain visible");
            Check(!backend.Texts.Exists(t => t.Value == "TAILS" || t.Value.StartsWith("TAILS ")),
                "zero-probability Tails details and banner are hidden");

            var handGame = Game.NewSandbox(new SandboxConfig { Coins = new List<string> { "normal", "safeport" }, Seed = 6612 });
            handGame.Mulligan = new Mulligan { Hand = new List<int>() };
            foreach (var coin in handGame.Coins) handGame.Mulligan.Hand.Add(coin.Uid);
            handGame.Encounter.Queue.Clear();
            handGame.Encounter.Pile.Clear();
            handGame.Dealt = null;
            handGame.Pending = null;
            Ui.Game = handGame;
            Ui.Holding = false;
            backend.Texts.Clear();
            AppCore.Draw();
            Check(!backend.Texts.Exists(t => t.Value.StartsWith("T ")),
                "opening-hand cards omit the effect line for an impossible Tails result");

            Ui.Game = null;
            Ui.Screen = "collection";
            Ui.CollectionCategory = "coins";
            Ui.CollectionFilter = "ALL";
            Ui.CollectionSort = "order";
            Ui.Profile.Collected.Add(CoinCatalog.SafePort.Id);
            int safePortIndex = Content.CoinOrder.FindIndex(coin => coin.Id == CoinCatalog.SafePort.Id);
            Check(safePortIndex >= 0, "Safe Port is present in the collection order");
            Ui.CollectionPage = safePortIndex / 15 + 1;
            var platform = (HeadlessPlatform)Ui.Platform;
            platform.X = 215 + (safePortIndex % 5) * 170 + 80;
            platform.Y = 260 + ((safePortIndex % 15) / 5) * 154 + 44;
            backend.Texts.Clear();
            AppCore.Draw();
            Check(Ui.HoveredCoin != null && Ui.HoveredCoin.Id == CoinCatalog.SafePort.Id,
                "Safe Port can be hovered from the collection grid");
            Check(!backend.Texts.Exists(t => t.Value == "TAILS" || t.Value.StartsWith("TAILS ")),
                "collection tooltip omits Safe Port's impossible Tails outcome");
            Check(!backend.Texts.Exists(t => t.Value == "ADDITIONAL EFFECT"),
                "collection tooltip omits an empty additional-effect section");

            int doublerIndex = Content.CoinOrder.FindIndex(coin => coin.Id == CoinCatalog.Doubler.Id);
            Ui.Profile.Collected.Add(CoinCatalog.Doubler.Id);
            Ui.CollectionPage = doublerIndex / 15 + 1;
            platform.X = 215 + (doublerIndex % 5) * 170 + 80;
            platform.Y = 260 + ((doublerIndex % 15) / 5) * 154 + 44;
            backend.Texts.Clear();
            AppCore.Draw();
            var headsLabel = backend.Texts.Find(t => t.Value == "HEADS");
            Check(headsLabel.Value != null && backend.Texts.Exists(t => t.Value.Contains("doubled for every Doubler")),
                "collection tooltip uses the custom Heads description for a stateful score");

            int cheerleaderIndex = Content.CoinOrder.FindIndex(coin => coin.Id == CoinCatalog.Cheerleader.Id);
            Ui.Profile.Collected.Add(CoinCatalog.Cheerleader.Id);
            Ui.CollectionPage = cheerleaderIndex / 15 + 1;
            platform.X = 215 + (cheerleaderIndex % 5) * 170 + 80;
            platform.Y = 260 + ((cheerleaderIndex % 15) / 5) * 154 + 44;
            backend.Texts.Clear();
            AppCore.Draw();
            headsLabel = backend.Texts.Find(t => t.Value == "HEADS");
            var buffsLabel = backend.Texts.Find(t => t.Value == "BUFFS");
            Check(headsLabel.Value != null && buffsLabel.Value != null && headsLabel.Y < buffsLabel.Y &&
                backend.Texts.Exists(t => t.Value.Contains("20%")),
                "collection tooltip generates a separate typed Buffs section after outcomes");

            int echoIndex = Content.CoinOrder.FindIndex(coin => coin.Id == CoinCatalog.TrueEcho.Id);
            Ui.Profile.Collected.Add(CoinCatalog.TrueEcho.Id);
            Ui.CollectionPage = echoIndex / 15 + 1;
            platform.X = 215 + (echoIndex % 5) * 170 + 80;
            platform.Y = 260 + ((echoIndex % 15) / 5) * 154 + 44;
            backend.Texts.Clear();
            AppCore.Draw();
            Check(backend.Texts.Exists(t => t.Value == "SPECIAL RULE"),
                "collection tooltip shows an explicit description for hook-driven behavior");
        }
        finally
        {
            CoinCatalog.Normal.On.Coins.Odds -= forceHeads;
            Hooks.Unbind();
            Ui.Game = null;
            Ui.Holding = false;
            RuntimeMode.Configure(false, false);
            ((HeadlessBackend)Gfx.Backend).Capture = false;
        }
    }

    static void CollectionCatalogCategories()
    {
        var oldProfile = Ui.Profile;
        var backend = (HeadlessBackend)Gfx.Backend;
        try
        {
            Ui.Profile = Profile.New();
            Ui.Game = null;
            Ui.Screen = "collection";
            Ui.CollectionPage = 1;
            Ui.CollectionFilter = "ALL";
            Ui.CollectionSort = "order";
            Ui.Holding = false;
            backend.Capture = true;

            Ui.CollectionCategory = "coins";
            AppCore.Draw();
            var chipsTab = Ui.Buttons.Find(button => button.Label == "CHIPS");
            Check(chipsTab != null, "collection categories are keyboard and controller focusable");
            chipsTab.Action();
            Check(Ui.CollectionCategory == "items" && Ui.CollectionPage == 1, "selecting the Chips tab changes category and resets paging");

            backend.Images.Clear(); backend.Texts.Clear();
            AppCore.Draw();
            Check(backend.Images.ContainsKey("items/force_heads") && backend.Texts.Exists(t => t.Value == "Force Heads"),
                "collection shows chip artwork and names");

            Ui.CollectionCategory = "relics";
            backend.Images.Clear(); backend.Texts.Clear();
            AppCore.Draw();
            Check(backend.Images.ContainsKey("relics/magnet") && backend.Texts.Exists(t => t.Value == "Magnet"),
                "collection shows relic artwork and names");

            Ui.CollectionCategory = "characters";
            backend.Images.Clear(); backend.Texts.Clear();
            AppCore.Draw();
            Check(backend.Images.ContainsKey("characters/blade") && backend.Texts.Exists(t => t.Value == "The Blade"),
                "collection shows character portraits and names");

            Ui.CollectionCategory = "coins";
            Ui.Profile.Collected.Add(CoinCatalog.Normal.Id);
            Ui.CollectionPage = 1;
            Ui.CollectionSort = "order";
            var platform = (HeadlessPlatform)Ui.Platform;
            platform.X = 295;
            platform.Y = 304;
            backend.Texts.Clear();
            AppCore.Draw();
            Check(Ui.HoveredCoin != null && Ui.HoveredCoin.Id == CoinCatalog.Normal.Id &&
                backend.Texts.Exists(t => t.Value == "UPGRADES") &&
                backend.Texts.Exists(t => t.Value == "LUCKY DAY") &&
                backend.Texts.Exists(t => t.Value == "MATHEMATICIAN"),
                "coin collection hover lists all its upgrades together in one section");
        }
        finally
        {
            Ui.Profile = oldProfile;
            Ui.CollectionCategory = "coins";
            backend.Capture = false;
            Ui.Game = null;
        }
    }

    static void CoinTypeColorPalette()
    {
        foreach (CoinType type in Enum.GetValues(typeof(CoinType)))
        {
            var color = D.CoinTypeColor(type);
            Check(DefinitionKeys.CoinColorHex(type).Length == 6 && color.R >= 0 && color.R <= 1,
                "every coin type has a valid RGB palette color");
        }
        var steel = D.CoinTypeColor(CoinType.Steel);
        Check(steel.R > .7f && Math.Abs(steel.R - steel.G) < .03f && Math.Abs(steel.G - steel.B) < .03f,
            "Steel uses a gray coin-type color");
        Check(D.CoinTypeColor(CoinType.Blood).R > D.CoinTypeColor(CoinType.Blood).G,
            "Blood uses its red coin-type color");
    }

    static void TitleAndEncounterScreens()
    {
        RuntimeMode.Configure(false, false);
        Ui.Game = null;
        Ui.Tutorial = null;
        Ui.EncounterReveal = null;
        Ui.Screen = "title";
        Check(Ui.UiImages.ContainsKey("title_scene"), "the original title scene with its three coins is loaded");
        Ui.Platform.WriteSave("run.json", "normal-run-checkpoint");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "PLAY" && b.X == 100 && b.Y == 240 && b.W == 340 && b.H == 52),
            "title actions match the Lua menu layout");
        Check(!Ui.Buttons.Exists(b => b.Label == "DEVELOPER MODE"), "title menu keeps Developer Mode hidden");
        AppCore.MousePressed(100, 630);
        AppCore.MousePressed(100, 630);
        AppCore.MousePressed(100, 630);
        Check(RuntimeMode.Dev && Ui.Screen == "select" && Ui.Profile.Collected.Count == Content.CoinOrder.Count && !A.HasSavedRun(),
            "triple-clicking the title version opens Developer Mode with the full collection");
        Check(Ui.Platform.ReadSave(RuntimeMode.DeveloperModePreference) == "1", "enabling Developer Mode persists the preference");
        A.Go("options");
        Ui.OptionsTab = "game";
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "EXIT DEVELOPER MODE"), "Options contains the relocated Developer Mode button");
        AppCore.MousePressed(640, 668);
        Check(!RuntimeMode.Dev && Ui.Screen == "title" && A.HasSavedRun(),
            "the Options button exits Developer Mode and preserves the normal saved run");
        Check(Ui.Platform.ReadSave(RuntimeMode.DeveloperModePreference) == "0", "exiting Developer Mode persists the preference");
        A.DeleteRun();

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
        Check(Ui.Game.Mulligan == null && Ui.Game.Phase == Phase.Encounter, "normal play automatically opens the bank without a contract");
        Check(A.HasSavedRun(), "the untouched opening bank is saved before setup finishes");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "CENTRAL COIN"), "the central coin exposes flip/advance");
        A.NextOrFlip();
        A.Update(.1);
        Check(Ui.Game.Pending != null && A.LoadRun() && Ui.Game.Pending == null && Ui.Game.Mulligan == null,
            "continuing during a flip restores the untouched level and a playable bank");
        Check(!Ui.Game.ContractsEnabled, "continued normal runs disable contract offers");
        A.DeleteRun();
        Ui.Game = null;
        Ui.EncounterReveal = null;
    }

    static void SeedSweep(int first, int count)
    {
        string[] characters = { "blade", "trader", "seer", "tinkerer", "naturalist", "conductor" };
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
