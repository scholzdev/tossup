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
        HandCoinsFlipInAnyOrder();
        ImpossibleOddsHideUnavailableSide();
        CollectionCatalogCategories();
        ShopCoinServices();
        CoinTypeColorPalette();
        PotOfGreedRewards();
        TitleAndEncounterScreens();
        DiscardParity();
        EncounterRevealFlow();
        int seeds=1000;
        if(int.TryParse(Environment.GetEnvironmentVariable("TOSSUP_SEED_SWEEP"),out var requested))seeds=Math.Max(1,requested);
        SeedSweep(1, seeds);
        Console.WriteLine("features: saves, tutorial, modes, reveal and "+seeds+" full-run seed sweep passed");
    }

    static void PotOfGreedRewards()
    {
        var outcomes = new[]
        {
            (Side.Heads, Rarity.Common),
            (Side.Tails, Rarity.Rare),
            (Side.Tie, Rarity.Epic),
        };
        foreach (var (side, rarity) in outcomes)
        {
            var game = Game.NewSandbox(new SandboxConfig
            {
                Coins = new List<string> { "potofgreed", "normal", "normal", "normal", "normal", "normal" },
                Seed = 801,
            });
            var coin = game.Coins[0];
            Check(coin.Definition.HasHook(nameof(CoinDef.OnResolve)), "Pot of Greed registers its resolve hook");
            coin.Definition.OnResolve(game, coin, new Res { Result = side });
            Check(game.Coins.Count == 8, "Pot of Greed grants two coins");
            Check(game.Coins[6].Definition.Rarity == rarity && game.Coins[7].Definition.Rarity == rarity &&
                game.Coins[6].Id != coin.Id && game.Coins[7].Id != coin.Id,
                "Pot of Greed grants the rarity shown for " + side);
            Check(game.Encounter.Hand.Count + game.Encounter.Pouch.Count == 6, "new coins join the pouch only from the next fight");
        }
        var nearlyFull = new List<string> { "potofgreed" };
        for (int i = 1; i < Game.DeckMax - 2; i++) nearlyFull.Add("normal");
        var full = Game.NewSandbox(new SandboxConfig { Coins = nearlyFull, Seed = 802 });
        var pot = full.Coins[0];
        pot.Definition.OnResolve(full, pot, new Res { Result = Side.Heads });
        pot.Definition.OnResolve(full, pot, new Res { Result = Side.Heads });
        Check(full.Coins.Count == Game.DeckMax, "Pot of Greed respects the pouch limit");
    }

    static void DiscardParity()
    {
        RuntimeMode.Configure(false, false);
        var game = Game.New(77101, "trader", null, new List<CoinDef> { CoinCatalog.Loaded, CoinCatalog.Normal, CoinCatalog.Normal });
        var hand = game.Encounter.Hand;
        Check(hand.Count == Game.HandSize, "a fight starts with a full hand");
        int fuseUid = hand[0];
        var fuse = Game.GetCoin(game, fuseUid);
        fuse.Definition = CoinCatalog.Fuse;
        Check(Game.Discard(game, new List<int> { fuseUid }) == 1 && game.Encounter.Discard.Contains(fuseUid) && !hand.Contains(fuseUid),
            "discarding sends a hand coin to the discard pile");
        Check(fuse.Charge == 6, "discarding fires the coin discard hook and growth");

        Ui.Game = game;
        Ui.FlipAnimation = null;
        Ui.EncounterReveal = null;
        game.Encounter.BankDiscards = 1;
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "DISCARD MODE: OFF"), "Crystal Ball exposes an explicit discard-mode toggle");
        Check(Ui.Buttons.Exists(b => b.Label == "HAND COIN"), "hand coins remain flippable while a discard is armed");
        var toggle = Ui.Buttons.Find(b => b.Label == "DISCARD MODE: OFF");
        toggle.Action();
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "DISCARD MODE: ON"), "discard mode can be enabled");
        var targets = Ui.Buttons.FindAll(b => b.Label == "HAND COIN");
        Check(targets.Count == hand.Count && targets.Count > 1, "discard mode makes every hand coin a target");
        int discardUid = hand[1];
        targets[1].Action();
        Check(game.Encounter.BankDiscards == 0 && game.Encounter.Discards == 2 && !hand.Contains(discardUid) && game.Encounter.Discard.Contains(discardUid),
            "a hand discard spends the Crystal Ball charge");
        Check(!Ui.DiscardMode, "discarding leaves discard mode");
        Ui.Game = null;
    }

    // A fight lasts five rounds; the hand refills after each; the pouch reshuffles the discards when it runs dry.
    static void HandCoinsFlipInAnyOrder()
    {
        RuntimeMode.Configure(false, false);
        var game = Game.New(6610, "blade", null, null);
        Ui.Game = game;
        Ui.FlipAnimation = null;
        Ui.EncounterReveal = null;
        Ui.Deciding = false;
        Ui.ResolveTimer = 0;
        AppCore.Draw();
        var cards = Ui.Buttons.FindAll(b => b.Label == "HAND COIN");
        Check(cards.Count == Game.HandSize && Ui.Buttons.Exists(b => b.Label == "END ROUND"), "every hand coin and the round button are clickable");
        var hand = new List<int>(game.Encounter.Hand);
        cards[2].Action();
        Check(game.Pending != null && game.Pending.Uid == hand[2] && Ui.FlipAnimation != null, "clicking a hand coin flips that coin");
        Ui.FlipAnimation = null;
        A.FlipCoin(hand[0]);
        Check(game.Encounter.Flips == 1 && game.LastResult.Uid == hand[2] && game.Pending != null && game.Pending.Uid == hand[0],
            "clicking the next coin keeps the last result and flips the next one, in the order chosen");
        Ui.FlipAnimation = null;
        A.EndRound();
        Check(game.Pending == null && game.Encounter.Round == 2 && game.Encounter.Hand.Count == Game.HandSize &&
            game.Encounter.Hand.Contains(hand[1]) && game.Encounter.Flips == 2, "ending the round keeps unflipped coins and refills the hand");
        Ui.Game = null;
    }

    static void SavedRunRoundTrip()
    {
        var game = Game.New(4401, "trader", new List<string> { "loaded" }, new List<CoinDef> { CoinCatalog.Loaded, CoinCatalog.Normal }, 3);
        game.Player.Gold = 37;
        game.Augments.Add("bankers_cut");
        game.Augments.Add("type_specialist");
        game.AugmentData["type_specialist"] = "fortune";
        game.Encounter.ComboPot = 6;
        string json = RunSave.Encode(game);
        var restored = RunSave.Decode(json);
        Check(restored != null, "valid saved run restores");
        Check(restored.Seed == game.Seed && restored.RngState == game.RngState, "RNG state survives restoration");
        Check(restored.CharacterId == "trader" && restored.Stake == 3 && restored.Player.Gold == 37, "run identity survives restoration");
        Check(restored.Coins.Count == Game.PouchSize && restored.Coins[0].Id == "loaded", "owned coin state survives restoration");
        Check(restored.Augments.Contains("bankers_cut") && restored.AugmentData["type_specialist"] == "fortune", "augment state survives restoration");
        Check(restored.Encounter.ComboPot == 6 && restored.Encounter.Hand.Count == Game.HandSize && restored.Pending == null, "safe-point encounter state survives restoration");
        Check(RunSave.Decode("{\"version\":999}") == null && RunSave.Decode("broken") == null, "damaged or incompatible saves are rejected");
        var unsafeGame=Game.New(4402,"blade");
        Game.Flip(unsafeGame,unsafeGame.Encounter.Hand[0]);
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
        Check(Ui.Game.Seed == 7 && Ui.Game.Coins.Count == Game.PouchSize && Ui.Game.Items.Contains("energy_drink"), "tutorial scene is deterministic");
        Check(Tutorial.Count == 12 && Tutorial.StepIndex == 1, "tutorial exposes every guided step");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "SKIP"), "tutorial overlay draws a skip control");
        Tutorial.Next();
        Check(Tutorial.StepIndex == 2, "tutorial advances manually");
        Tutorial.Finish();
        Check(Ui.Game == null && Ui.Tutorial == null && Ui.Screen == UiScreen.Title, "tutorial exits without retaining its run");
    }

    // A UI run starts on the map; step onto the first table to reach the encounter.
    static void FirstFight() { var g = Ui.Game; Check(g.Phase == Phase.Map && Game.ChooseNode(g, g.Map.FindIndex(n => n.Row == 0)), "a new run starts on the map"); }

    static void RuntimeModesAndSandbox()
    {
        RuntimeMode.Configure(true, false);
        var clockGame = new GameState { Phase = Phase.Encounter, Encounter = new Encounter() };
        Game.AdvanceClock(clockGame, 1.5);
        Check(Math.Abs(clockGame.Encounter.ElapsedSeconds - 1.5) < 1e-9, "active encounter time advances");
        clockGame.Paused = true;
        Game.AdvanceClock(clockGame, 2);
        Check(Math.Abs(clockGame.Encounter.ElapsedSeconds - 1.5) < 1e-9, "paused encounter time stops");
        var dev = Game.New(5501, "blade", null, null);
        Check(RuntimeMode.Dev && dev.Player.Gold == 5000, "developer mode grants test gold");
        var profile = Profile.New();
        RuntimeMode.ApplyProfile(profile);
        foreach (var id in Content.CharacterOrder)
            Check(Profile.CharacterUnlocked(profile, id), "developer mode unlocks character " + id);
        foreach (var coin in Content.CoinOrder)
        {
            int level = Profile.MasteryLevel(profile, coin);
            double progress = Profile.MasteryProgress(profile, coin);
            Check(level >= 0 && level <= coin.Mastery.Thresholds.Length &&
                progress == (level == 0 ? 0 : coin.Mastery.Thresholds[level - 1]),
                "developer mode assigns a whole mastery level to " + coin.Id);
        }

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
            Ui.SelectedCharacter=Content.Characters["blade"];Ui.Tutorial=null;Ui.Confirm=null;
            A.Start(6601);FirstFight();
            Check(Ui.Game.RunEncounter!=null && Ui.EncounterReveal!=null, "normal and developer Start show the assigned encounter");
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

    static void ImpossibleOddsHideUnavailableSide()
    {
        RuntimeMode.Configure(false, false);
        Action<HookContext, Odds> forceHeads = (ctx, odds) => { odds.Heads = 1; odds.Edge = 0; };
        CoinCatalog.Normal.On.Coins.Odds += forceHeads;
        try
        {
            var game = Game.NewSandbox(new SandboxConfig { Coins = new List<string> { "normal", "safeport" }, Seed = 6611 });
            var lastCoin = game.Coins[0];
            game.Coins.Remove(lastCoin);
            game.Encounter.Hand.Remove(lastCoin.Uid);
            game.Encounter.RoundLog.Add(new RoundFlip { Uid = lastCoin.Uid, CoinId = lastCoin.Id, Result = Side.Heads, Points = 2 });
            Ui.Game = game;
            Ui.FlipAnimation = null;
            var backend = (HeadlessBackend)Gfx.Backend;
            backend.Capture = true;
            backend.Texts.Clear();
            backend.Images.Clear();
            AppCore.Draw();
            Check(backend.Images.ContainsKey("coins/normal"), "a flipped coin keeps its card (and art) after the coin leaves play");
            Check(backend.Texts.Exists(t => t.Value == "HEADS"), "the flipped card still shows the side it landed on");
            Check(!backend.Texts.Exists(t => t.Value == "TAILS" || t.Value.StartsWith("TAILS ")),
                "zero-probability Tails details and banner are hidden");

            var handGame = Game.NewSandbox(new SandboxConfig { Coins = new List<string> { "normal", "safeport" }, Seed = 6612 });
            Ui.Game = handGame;
            backend.Texts.Clear();
            AppCore.Draw();
            Check(!backend.Texts.Exists(t => t.Value.StartsWith("T ")),
                "hand cards omit the effect line for an impossible Tails result");

            Ui.Game = null;
            Ui.Screen = UiScreen.Collection;
            Ui.CollectionCategory = "coins";
            Ui.CollectionFilter = CollectionRarityFilter.All;
            Ui.CollectionSort = CollectionSortMode.Order;
            Ui.Profile.Collected.Add(CoinCatalog.SafePort.Id);
            int safePortIndex = Content.CoinOrder.FindIndex(coin => coin.Id == CoinCatalog.SafePort.Id);
            Check(safePortIndex >= 0, "Safe Port is present in the collection order");
            CoinDetailView.Open(CoinCatalog.SafePort);
            backend.Texts.Clear();
            AppCore.Draw();
            Check(ReferenceEquals(CoinDetailView.SelectedCoin, CoinCatalog.SafePort),
                "Safe Port opens in the collection detail view");
            Check(!backend.Texts.Exists(t => t.Value == "TAILS" || t.Value.StartsWith("TAILS ")),
                "coin details omit Safe Port's impossible Tails outcome");
            Check(!backend.Texts.Exists(t => t.Value == "ADDITIONAL EFFECT"),
                "coin details omit an empty additional-effect section");

            Ui.Profile.Collected.Add(CoinCatalog.Doubler.Id);
            CoinDetailView.Open(CoinCatalog.Doubler);
            backend.Texts.Clear();
            AppCore.Draw();
            var headsLabel = backend.Texts.Find(t => t.Value.StartsWith("HEADS"));
            Check(headsLabel.Value != null && backend.Texts.Exists(t => t.Value.Contains("doubled for every Doubler")),
                "coin details use the custom Heads description for a stateful score");

            Ui.Profile.Collected.Add(CoinCatalog.Cheerleader.Id);
            CoinDetailView.Open(CoinCatalog.Cheerleader);
            backend.Texts.Clear();
            AppCore.Draw();
            headsLabel = backend.Texts.Find(t => t.Value.StartsWith("HEADS"));
            var buffsLabel = backend.Texts.Find(t => t.Value == "BUFFS");
            Check(headsLabel.Value != null && buffsLabel.Value != null && headsLabel.Y < buffsLabel.Y &&
                backend.Texts.Exists(t => t.Value.Contains("20%")),
                "coin details generate a separate typed Buffs section after outcomes");

            Ui.Profile.Collected.Add(CoinCatalog.TrueEcho.Id);
            CoinDetailView.Open(CoinCatalog.TrueEcho);
            backend.Texts.Clear();
            AppCore.Draw();
            Check(backend.Texts.Exists(t => t.Value == "SPECIAL RULE"),
                "coin details show an explicit description for hook-driven behavior");
        }
        finally
        {
            CoinCatalog.Normal.On.Coins.Odds -= forceHeads;
            Hooks.Unbind();
            Ui.Game = null;
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
            Ui.Screen = UiScreen.Collection;
            Ui.CollectionPage = 1;
            Ui.CollectionFilter = CollectionRarityFilter.All;
            Ui.CollectionSort = CollectionSortMode.Order;
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
            Ui.CollectionSort = CollectionSortMode.Order;
            var platform = (HeadlessPlatform)Ui.Platform;
            platform.X = -50;
            platform.Y = -50;
            backend.Texts.Clear();
            AppCore.Draw();
            var detail = Ui.Buttons.Find(button => button.Label == "DETAIL");
            Check(detail != null && Ui.HoveredCoin == null,
                "collection cards open details instead of crowding the grid with a tooltip");
            detail.Action();
            backend.Images.Clear(); backend.Texts.Clear();
            AppCore.Draw();
            Check(Ui.Screen == UiScreen.CoinDetail && ReferenceEquals(CoinDetailView.SelectedCoin, CoinCatalog.Normal) &&
                backend.Images.ContainsKey("coins/normal") &&
                backend.Images.ContainsKey("characters/blade") &&
                backend.Texts.Exists(t => t.Value == "MASTERY LEVEL 0 / 3") &&
                !backend.Texts.Exists(t => t.Value == "MASTERY LEVEL 1") &&
                backend.Texts.Exists(t => t.Value.StartsWith("HEADS")),
                "coin details show the coin, odds, left-side mastery and character portraits");
            var fullCoin = backend.Images["coins/normal"];
            platform.Clock += 2.2;
            backend.Images.Clear();
            AppCore.Draw();
            var idleCoin = backend.Images["coins/normal"];
            Check(Math.Abs(idleCoin[2] - idleCoin[0]) == Math.Abs(fullCoin[2] - fullCoin[0]),
                "coin detail preview remains static without a spin control");
            Check(!Ui.Buttons.Exists(button => button.Label == "SPIN"),
                "coin detail screen has no spin interaction");
            Profile.AddMastery(Ui.Profile, CoinCatalog.Normal, CoinCatalog.Normal.Mastery.Thresholds[1]);
            backend.Texts.Clear();
            AppCore.Draw();
            Check(backend.Texts.Exists(t => t.Value == "MASTERY LEVEL 2 / 3"),
                "coin detail view follows permanent coin mastery");
            AppCore.KeyPressed("escape");
            Check(Ui.Screen == UiScreen.Collection && Ui.CollectionPage == 1,
                "back from coin details returns to the same collection page");
        }
        finally
        {
            Ui.Profile = oldProfile;
            Ui.CollectionCategory = "coins";
            backend.Capture = false;
            Ui.Game = null;
        }
    }

    static void ShopCoinServices()
    {
        var game = Game.NewSandbox(new SandboxConfig { Coins = new List<string> { "normal" }, Gold = 50, Seed = 19 });
        Game.OpenSandboxShop(game);
        Ui.Game = game;
        Ui.Screen = UiScreen.Shop;
        AppCore.Draw();
        var tuneButtons = Ui.Buttons.FindAll(button => button.Label == "BUY" && button.X == 1102);
        Check(tuneButtons.Count == 0 && Ui.Buttons.Exists(button => button.Label == "REMOVE"),
            "shop only offers coin removal, without purchasable coin upgrades");
        Ui.Game = null;
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
        Ui.Screen = UiScreen.Title;
        Check(Ui.UiImages.ContainsKey("title_scene"), "the original title scene with its three coins is loaded");
        Ui.Platform.WriteSave("run.json", "normal-run-checkpoint");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "PLAY" && b.X == 100 && b.Y == 240 && b.W == 340 && b.H == 52),
            "title actions match the Lua menu layout");
        Check(!Ui.Buttons.Exists(b => b.Label == "DEVELOPER MODE"), "title menu keeps Developer Mode hidden");
        AppCore.MousePressed(100, 630);
        AppCore.MousePressed(100, 630);
        AppCore.MousePressed(100, 630);
        Check(RuntimeMode.Dev && Ui.Screen == UiScreen.Select && Ui.Profile.Collected.Count == Content.CoinOrder.Count && !A.HasSavedRun(),
            "triple-clicking the title version opens Developer Mode with the full collection");
        Check(Ui.Profile.CoinMastery.ContainsKey(CoinCatalog.Normal.Id),
            "developer mode assigns coin mastery when the profile loads");
        Ui.SelectedCharacter = Content.Characters["blade"];
        A.Start(6601);
        Check(ReferenceEquals(Ui.Game.MasteryProfile, Ui.Profile), "developer runs use their assigned mastery rewards");
        Ui.Game = null;
        Ui.EncounterReveal = null;
        Check(Ui.Platform.ReadSave(RuntimeMode.DeveloperModePreference) == "1", "enabling Developer Mode persists the preference");
        A.Go(UiScreen.Options);
        Ui.OptionsTab = "game";
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "EXIT DEVELOPER MODE"), "Options contains the relocated Developer Mode button");
        AppCore.MousePressed(640, 668);
        Check(!RuntimeMode.Dev && Ui.Screen == UiScreen.Title && A.HasSavedRun(),
            "the Options button exits Developer Mode and preserves the normal saved run");
        Check(Ui.Platform.ReadSave(RuntimeMode.DeveloperModePreference) == "0", "exiting Developer Mode persists the preference");
        A.DeleteRun();

        Ui.SelectedCharacter = Content.Characters["blade"];
        A.Start(6602);FirstFight();
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
        Check(Ui.Game.Phase == Phase.Encounter && Ui.Game.Encounter.Hand.Count == Game.HandSize, "normal play starts with a full hand");
        Check(A.HasSavedRun(), "the untouched opening hand is saved before the first flip");
        AppCore.Draw();
        Check(Ui.Buttons.Exists(b => b.Label == "HAND COIN") && Ui.Buttons.Exists(b => b.Label == "END ROUND"), "the hand and the round button are drawn");
        A.FlipFirst();
        A.Update(.1);
        Check(Ui.Game.Pending != null && A.LoadRun() && Ui.Game.Pending == null && Ui.Game.Encounter.Hand.Count == Game.HandSize && Ui.Game.Encounter.Flips == 0,
            "continuing during a flip restores the last saved point with the whole hand");
        A.DeleteRun();
        Ui.Game = null;
        Ui.EncounterReveal = null;
    }

    static void SeedSweep(int first, int count)
    {
        string[] characters = { "blade", "trader", "seer", "tinkerer", "naturalist", "conductor" };
        for (int seed = first; seed < first + count; seed++)
        {
            var game = Game.New(seed, characters[seed % characters.Length], null, null, 1 + seed % Game.Stakes.Count);
            int guard = 0;
            while (game.Phase != Phase.GameOver && game.Phase != Phase.Victory && guard++ < 2000)
            {
                if (game.Phase == Phase.Encounter)
                {
                    if (game.Pending != null) Game.Resolve(game);
                    else if (game.Encounter.Cleared) Game.EndLevel(game);
                    else
                    {
                        int uid = game.Encounter.Hand.Find(u => Game.CanFlip(game, u));
                        if (uid != 0) Game.Flip(game, uid);
                        else Game.EndRound(game);
                    }
                }
                else if (game.Phase == Phase.Shop)
                {
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
                Check(double.IsFinite(game.Player.Gold) && double.IsFinite(game.Player.Energy), "finite resources for seed " + seed);
                Check(game.Coins.Count > 0 && game.Coins.Count <= Game.DeckMax, "valid deck size for seed " + seed);
                if (game.Encounter != null) Check(double.IsFinite(game.Encounter.Scored) && double.IsFinite(game.Encounter.EnemyScore) &&
                    game.Encounter.Hand.Count + game.Encounter.Pouch.Count + game.Encounter.Discard.Count <= game.Coins.Count, "valid encounter state for seed " + seed);
            }
            Check(guard < 2000, "seed terminates without a rules loop: " + seed + " phase="+game.Phase+" level="+game.EncounterIndex+
                " clear="+game.Encounter?.Cleared+" pending="+(game.Pending!=null)+" round="+game.Encounter?.Round);
        }
    }
}
