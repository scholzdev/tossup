using System;
using System.Collections.Generic;
using Tossup;
using Tossup.UI;

static class LatestTests
{
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("latest rules: "+message);}
    static bool Near(double a,double b)=>Math.Abs(a-b)<1e-9;

    public static void Run()
    {
        Check(Content.CoinOrder.Count>=100&&Content.Coins.Count==Content.CoinOrder.Count,"all coins are registered");
        Check(Content.Characters["conductor"].Locked.Exists(c=>c.Id=="reprise"),"Reprise is a Conductor unlock");
        Check(Content.CharacterOrder.Count==6,"six characters are registered");
        var characterProfile=Profile.New();
        Check(Profile.CharacterUnlocked(characterProfile,Content.CharacterOrder[0]),"the first character starts unlocked");
        for(int i=1;i<Content.CharacterOrder.Count;i++)
            Check(!Profile.CharacterUnlocked(characterProfile,Content.CharacterOrder[i]),"later characters start locked");
        for(int i=0;i+1<Content.CharacterOrder.Count;i++)
        {
            Check(Profile.RecordWin(characterProfile,Content.CharacterOrder[i])==Content.CharacterOrder[i+1],"a win unlocks the next character");
            Check(Profile.CharacterUnlocked(characterProfile,Content.CharacterOrder[i+1]),"unlocked character can be selected");
        }
        foreach(string characterId in new[]{"tinkerer","naturalist","conductor"})
        {
            var loadout=Profile.Loadout(Profile.New(),characterId,Game.StartMax,Game.MaxCopies);
            var definitions=loadout.ConvertAll(id=>Content.Coins[id]);
            var characterRun=Game.New(900+Content.CharacterOrder.IndexOf(characterId),characterId,null,definitions,false);
            Check(loadout.Count>0&&loadout.Count<=Game.StartMax&&characterRun.Coins.Count==loadout.Count,"valid default deck starts for "+characterId);
            Check(Content.Characters[characterId].Perks.Count==2,"two visible perks are defined for "+characterId);
        }
        var stackedProfile = Profile.New();
        var mixedCommons = new List<string> { "normal", "sword", "dagger" };
        Check(Profile.CanAdd(stackedProfile, "blade", mixedCommons, "normal", 5, Game.MaxCopies),
            "different common coins do not consume Normal's copy limit");
        Check(Profile.LimitedSet(new List<string> { "normal", "normal", "normal", "sword", "dagger" }, 5)
                .Count == 5, "loadout repair keeps distinct common coins");
        Check(Game.New(901, "blade", null,
                new List<CoinDef> { CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Sword, CoinCatalog.Dagger }, false)
                .Coins.Count == 5, "run validation accepts stacked copies of distinct common coins");
        Check(!Profile.CanAdd(stackedProfile, "blade", new List<string> { "normal", "normal", "normal" }, "normal", 5, Game.MaxCopies),
            "a fourth copy of one common coin remains blocked");
        var tinkerer=Game.New(911,"tinkerer",null,null,false);
        Check(tinkerer.Player.Energy==tinkerer.Player.MaxEnergy+1,"Tinkerer starts each level with bonus energy");
        tinkerer.ShopOffers=new List<CoinDef>{Content.Coins["normal"]};
        Check(Game.CoinOfferCost(tinkerer,0)==Content.Coins["normal"].Cost-2,"Tinkerer discounts shop coin offers");
        var naturalist=Game.New(912,"naturalist",null,null,false);
        Check(naturalist.Encounter.ExtraExchanges==1&&Game.CharacterPerkValue(naturalist,CharacterPerkType.TailsGold)==1,"Naturalist gains tails gold and an extra exchange");
        var naturalistCoin=naturalist.Coins.Find(c=>c.Id=="normal");
        naturalist.Pending=new FlipState{Uid=naturalistCoin.Uid,CoinId=naturalistCoin.Id,Raw=Side.Tails,Result=Side.Tails};
        double naturalistGold=naturalist.Player.Gold;
        Check(Game.Resolve(naturalist)&&naturalist.Player.Gold==naturalistGold+1,"Naturalist earns perk gold when Tails resolves");
        var conductor=Game.New(913,"conductor",null,null,false);
        Check(Near(conductor.Encounter.ComboStep,Game.ComboStep+.2)&&conductor.Encounter.Shield==1,"Conductor improves combo growth and starts shielded");
        Check(Content.Characters["trader"].Pool.Contains(CoinCatalog.SafePort),"Safe Port is available to the Trader");
        Check(Content.ItemOrder.Count==11&&Content.Items.Count==11,"all 11 chips are registered");
        Check(Game.Route.Count==8&&Game.Route[7].Boss,"eight-stage route ends at The House");
        Check(Game.Stakes.Count==8&&Game.Modifiers.Count==8,"stakes and modifiers are complete");
        Check(Game.Contracts.Count==5&&Game.Encounters.Count==7&&Game.AugmentDefs.Count==8,"run systems are complete");
        for (int i = 0; i < EncounterCatalog.Ordered.Count; i++)
        {
            var definition = EncounterCatalog.Ordered[i];
            Check(Game.Encounters[i] == definition && Game.EncounterById[definition.Id] == definition &&
                definition.GetType() != typeof(RunEncounterDef), "encounter registry uses its own definition for " + definition.Id);
        }
        for (int i = 0; i < AugmentCatalog.Ordered.Count; i++)
        {
            var definition = AugmentCatalog.Ordered[i];
            Check(Game.AugmentOrder[i] == definition.Id && Game.AugmentDefs[definition.Id] == definition &&
                definition.GetType() != typeof(AugmentDef), "augment registry uses its own definition for " + definition.Id);
        }

        var slots=Game.New(101,"blade",null,null,false);
        slots.Phase=Phase.Shop;slots.Player.Gold=100;
        Check(slots.Slots==Game.StartMax&&Game.SlotPrice(slots)==5&&Game.BuySlot(slots)&&slots.Slots==Game.StartMax+1,"deck slot purchase");
        Check(slots.Player.Gold==95,"slot price is charged");

        var stake=Game.New(102,"blade",null,null,false,8);
        Check(Near(Game.Rule(stake,"quota_mult",1),1.8)&&Near(Game.Rule(stake,"exchange_max",3),2),"cumulative stake rules");

        var tied=Game.New(103,"trader",null,new List<CoinDef>{CoinCatalog.Loaded},false);
        var loaded=tied.Coins.Find(c=>c.Id=="loaded");
        Check(loaded!=null&&Near(Content.Coins["loaded"].TieProbability,.23)&&Game.TieEffects("loaded").Count==2,"Edge outcome data");

        var upgraded=Game.New(104,"blade",null,null,false);
        var normal=upgraded.Coins.Find(c=>c.Id=="normal");
        Check(Near(Game.Probability(upgraded,normal),.65),"base Heads odds are unchanged by removed upgrades");

        var contract=Game.New(105,"blade",null,null,true);
        Check(Game.OfferContract(contract)&&contract.Phase==Phase.Contract,"contract offer phase");
        string contractId=contract.Encounter.ContractOptions[0];
        Check(Game.ChooseContract(contract,contractId)&&contract.Encounter.Contract.Id==contractId,"contract selection");

        var bet=Game.New(106,"blade",null,null,false);bet.Player.Gold=50;
        var quote=Game.SideBetQuote(bet,Side.Heads);
        Check(quote!=null&&Game.PlaceSideBet(bet,Side.Heads)&&bet.Player.Gold==50-quote.Stake,"side bet placement");
        bet.Augments.Add(AugmentCatalog.HedgeFund.Id);
        Check(Game.SideBetQuote(bet,Side.Heads).Payout==(int)Math.Floor(quote.Payout*1.25+.5),
            "Hedge Fund definition adjusts a side bet quote");

        var augment=Game.New(107,"blade",null,null,false);augment.Phase=Phase.Shop;augment.EncounterIndex=2;
        Check(Game.OfferAugment(augment,3)&&augment.Phase==Phase.Augment&&augment.AugmentOptions.Count==3,"augment offer phase");
        string augmentId=augment.AugmentOptions[0];Check(Game.ChooseAugment(augment,augmentId),"augment selection");
        if(augment.AugmentPending!=null){var choices=Game.AugmentChoices(augment);Check(choices.Count>0&&Game.ChooseAugmentOption(augment,choices[0].Key),"augment follow-up choice");}
        Check(augment.Augments.Contains(augmentId),"augment is owned");

        var flow=Game.New(108,"blade",null,null,false);
        while(flow.Phase!=Phase.Victory)
        {
            Check(flow.Phase==Phase.Encounter,"route enters an encounter");flow.Encounter.Cleared=true;flow.Pending=null;flow.Mulligan=null;
            Check(Game.EndLevel(flow),"cleared level closes");if(flow.Phase==Phase.Victory)break;
            Check(Game.NextEncounter(flow),"route advances");
            if(flow.Phase==Phase.Augment){string id=flow.AugmentOptions[0];Check(Game.ChooseAugment(flow,id),"route augment chosen");if(flow.AugmentPending!=null){var choices=Game.AugmentChoices(flow);Check(choices.Count>0&&Game.ChooseAugmentOption(flow,choices[0].Key),"route augment option chosen");}}
        }
        Check(flow.EncounterIndex==8,"route reaches the eighth-stage boss");

        var contractView=Game.New(109,"blade",null,null,true);Game.OfferContract(contractView);Ui.Game=contractView;AppCore.Draw();
        var augmentView=Game.New(110,"blade",null,null,false);augmentView.Phase=Phase.Shop;augmentView.EncounterIndex=2;Game.OfferAugment(augmentView,3);Ui.Game=augmentView;AppCore.Draw();Ui.Game=null;

        var profile=Profile.New();Check(Profile.MaxStake(profile,"blade")==1,"new profile starts on stage 1");
        Check(Profile.RecordStakeWin(profile,"blade",1)==2&&Profile.MaxStake(profile,"blade")==2,"winning unlocks next stake");
        string json=Profile.Encode(profile);var restored=Profile.Decode(json);
        Check(Profile.MaxStake(restored,"blade")==2,"stake progression survives JSON");
        CheckCoinMastery();
        CheckMasteryCoverage();
        CheckSettingsPersistence();
        FeatureParityTests.Run();
        Console.WriteLine("latest: focused gameplay checks passed");
    }

    static void CheckCoinMastery()
    {
        var profile = Profile.New();
        var normal = CoinCatalog.Normal;
        Check(normal.Mastery.Thresholds[0] == 10 && normal.Mastery.Thresholds[2] == 150,
            "per-coin mastery goals are halved");
        Profile.AddMastery(profile, normal, 12);
        Profile.AddMastery(profile, normal, 8);
        Check(Profile.MasteryLevel(profile, normal) == 1 && profile.MasteryDirty,
            "coin-specific progress accumulates across sessions");
        Ui.Profile = profile;
        Check(D.CoinMasteryOutcomeDescription(normal, Side.Heads)?.Contains("Heads scores +1") == true &&
            D.CoinMasteryOutcomeDescription(normal, Side.Tails) == null,
            "outcome cards show earned side rewards only");
        var restored = Profile.Decode(Profile.Encode(profile));
        Check(Profile.MasteryProgress(restored, normal) == 20 && Profile.MasteryLevel(restored, normal) == 1,
            "mastery survives profile save and load");
        Profile.AddMastery(restored, normal, 1000);
        Check(Profile.MasteryLevel(restored, normal) == 3 && Profile.MasteryProgress(restored, normal) == normal.Mastery.Thresholds[2],
            "mastery caps at level three");
        Ui.Profile = restored;
        Check(D.CoinMasteryOutcomeDescription(normal, Side.Heads)?.Contains("first Heads") == true &&
            D.CoinMasteryOutcomeDescription(normal, Side.Tails)?.Contains("Tails scores 1") == true,
            "conditional and Tails mastery rewards appear on their own outcome cards");

        var loadedProfile = Profile.New();
        Profile.AddMastery(loadedProfile, CoinCatalog.Loaded, CoinCatalog.Loaded.Mastery.Thresholds[2]);
        var loadedRun = Game.New(1200, "trader", null, new List<CoinDef> { CoinCatalog.Loaded }, false);
        loadedRun.MasteryProfile = loadedProfile;
        var loadedCoin = loadedRun.Coins[0];
        Hooks.Bind(loadedRun, loadedCoin);
        Check(Near(Game.Probability(loadedRun, loadedCoin), .67), "Loaded mastery adds eight points of Heads chance");
        loadedRun.Pending = new FlipState { Uid = loadedCoin.Uid, CoinId = loadedCoin.Id, Raw = Side.Heads, Result = Side.Heads };
        double loadedGold = loadedRun.Player.Gold;
        Check(Game.Resolve(loadedRun) && loadedRun.Player.Gold == loadedGold + 7,
            "Loaded mastery adds three gold to its Heads payout");

        var run = Game.New(1201, "blade", null, new List<CoinDef> { normal }, false);
        run.MasteryProfile = restored;
        var coin = run.Coins.Find(c => c.Id == normal.Id);
        Hooks.Bind(run, coin);
        run.Pending = new FlipState { Uid = coin.Uid, CoinId = coin.Id, Raw = Side.Heads, Result = Side.Heads };
        Check(Game.Resolve(run) && run.LastResult.Gained >= 5,
            "unlocked Normal rewards apply through its Resolve hook");
        int firstLevel = coin.MasteryFirstHeadsLevel;
        Check(firstLevel == run.EncounterIndex, "first-Heads reward is marked once per level");

        var steelProfile = Profile.New();
        var steelRun = Game.New(1202, "blade", new List<string> { "whetstone", "sword" },
            new List<CoinDef> { CoinCatalog.Whetstone, CoinCatalog.Sword }, false);
        steelRun.MasteryProfile = steelProfile;
        var stone = steelRun.Coins.Find(c => c.Id == "whetstone");
        var sword = steelRun.Coins.Find(c => c.Id == "sword");
        Hooks.Bind(steelRun, stone);
        steelRun.Pending = new FlipState { Uid = stone.Uid, CoinId = stone.Id, Raw = Side.Heads, Result = Side.Heads };
        Check(Game.Resolve(steelRun), "Whetstone creates its Steel buff");
        Hooks.Bind(steelRun, sword);
        steelRun.Pending = new FlipState { Uid = sword.Uid, CoinId = sword.Id, Raw = Side.Heads, Result = Side.Heads };
        Check(Game.Resolve(steelRun) && Profile.MasteryProgress(steelProfile, CoinCatalog.Whetstone) == 1,
            "Steel buff usage credits its source coin");
        Profile.AddMastery(steelProfile, CoinCatalog.Whetstone, 120);
        var masteredSteel = Game.New(1204, "blade", new List<string> { "whetstone", "sword" },
            new List<CoinDef> { CoinCatalog.Whetstone, CoinCatalog.Sword }, false);
        masteredSteel.MasteryProfile = steelProfile;
        var masteredStone = masteredSteel.Coins.Find(c => c.Id == "whetstone");
        Hooks.Bind(masteredSteel, masteredStone);
        masteredSteel.Pending = new FlipState { Uid = masteredStone.Uid, CoinId = masteredStone.Id, Raw = Side.Heads, Result = Side.Heads };
        Check(Game.Resolve(masteredSteel), "mastered Whetstone creates a buff");
        var masteredBuff = masteredSteel.Encounter.Buffs.Find(b => b.SpecId == "whetstone_steel");
        Check(masteredBuff != null && masteredBuff.Amount == 4 && masteredBuff.PenaltyAmount == 1 && masteredBuff.Left == 3,
            "all Whetstone mastery rewards modify its sourced buff");

        var fuseProfile = Profile.New();
        Profile.AddMastery(fuseProfile, CoinCatalog.Fuse, 100);
        var fuseRun = Game.New(1203, "blade", new List<string> { "fuse" }, new List<CoinDef> { CoinCatalog.Fuse }, false);
        fuseRun.MasteryProfile = fuseProfile;
        var fuse = fuseRun.Coins.Find(c => c.Id == "fuse");
        fuse.Charge = 10;
        Hooks.Bind(fuseRun, fuse);
        fuseRun.Pending = new FlipState { Uid = fuse.Uid, CoinId = fuse.Id, Raw = Side.Heads, Result = Side.Heads };
        double energy = fuseRun.Player.Energy;
        Check(Game.Resolve(fuseRun) && fuseRun.Player.Energy == energy + 1 && fuse.Charge == 0,
            "Fuse level two adds energy when ten charge is spent");
        Profile.AddMastery(fuseProfile, CoinCatalog.Fuse, 200);
        var masteredFuseRun = Game.New(1205, "blade", new List<string> { "fuse" }, new List<CoinDef> { CoinCatalog.Fuse }, false);
        masteredFuseRun.MasteryProfile = fuseProfile;
        var masteredFuse = masteredFuseRun.Coins.Find(c => c.Id == "fuse");
        masteredFuse.Charge = 10;
        Hooks.Bind(masteredFuseRun, masteredFuse);
        masteredFuseRun.Pending = new FlipState { Uid = masteredFuse.Uid, CoinId = masteredFuse.Id, Raw = Side.Heads, Result = Side.Heads };
        Check(Game.Resolve(masteredFuseRun) && masteredFuse.Charge == 2,
            "Fuse level three retains a fifth of spent charge");
    }

    static void CheckMasteryCoverage()
    {
        int seed = 1300;
        foreach (var definition in Content.CoinOrder)
        {
            Check(definition.Mastery != null && definition.Mastery.Thresholds.Length == 3 &&
                definition.Mastery.Rewards.Length == 3 && definition.Mastery.RewardSides.Length == 3,
                "three mastery levels exist for " + definition.Id);
            foreach (var side in new[] { Side.Heads, Side.Tails, Side.Tie })
            {
                var sandbox = new SandboxConfig { Coins = new List<string> { definition.Id } };
                var game = Game.New(seed++, "blade", null, null, false, sandbox: sandbox);
                var profile = Profile.New();
                Profile.AddMastery(profile, definition, definition.Mastery.Thresholds[2]);
                game.MasteryProfile = profile;
                var coin = game.Coins[0];
                Hooks.Bind(game, coin);
                game.Pending = new FlipState { Uid = coin.Uid, CoinId = coin.Id, Raw = side, Result = side };
                Check(Game.Resolve(game), "mastered " + definition.Id + " resolves " + side);
            }
        }
    }

    static void CheckSettingsPersistence()
    {
        RuntimeMode.Configure(false, false);
        string savedProfile = Ui.Platform.ReadSave("profile.json");
        var options = new Options
        {
            ScreenShake = false,
            FastFlip = true,
            Fullscreen = true,
            SeenHelp = true,
            Language = "de",
            VolumeMaster = 30,
            VolumeMusic = 20,
            VolumeSfx = 10,
        };
        Ui.Profile = Profile.New();
        Ui.Profile.Options = options;
        A.SaveOptions();

        Ui.Profile = Profile.New();
        A.LoadProfile();
        var loaded = Ui.Profile.Options;
        var platform = (HeadlessPlatform)Ui.Platform;
        Check(!loaded.ScreenShake && loaded.FastFlip && loaded.Fullscreen && loaded.SeenHelp &&
            loaded.Language == "de" && loaded.VolumeMaster == 30 && loaded.VolumeMusic == 20 && loaded.VolumeSfx == 10,
            "all preferences restore from the saved profile on startup");
        Check(platform.Fullscreen && Math.Abs(platform.MusicVolume - .036f) < .00001f,
            "startup applies saved fullscreen and music volume to the platform");

        A.SetVolume("volume_sfx", 15);
        var written = Profile.Decode(Ui.Platform.ReadSave("profile.json"));
        Check(written.Options.VolumeSfx == 15, "sound slider changes persist immediately");
        Ui.Profile = Profile.New();
        A.LoadProfile();
        Check(Ui.Profile.Options.VolumeSfx == 15, "sound slider value survives profile reload");

        if (savedProfile == null) Ui.Platform.DeleteSave("profile.json");
        else Ui.Platform.WriteSave("profile.json", savedProfile);
        A.LoadProfile();
    }
}
