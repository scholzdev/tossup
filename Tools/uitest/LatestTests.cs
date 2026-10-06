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
        Check(Content.CoinOrder.Count==100&&Content.Coins.Count==100,"all 100 coins are registered");
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
        normal.Upgrade=UpgradeCatalog.Mathematician;
        Check(Near(Game.Probability(upgraded,normal),.75),"coin upgrade changes Heads odds");

        var contract=Game.New(105,"blade",null,null,true);
        Check(Game.OfferContract(contract)&&contract.Phase==Phase.Contract,"contract offer phase");
        string contractId=contract.Encounter.ContractOptions[0];
        Check(Game.ChooseContract(contract,contractId)&&contract.Encounter.Contract.Id==contractId,"contract selection");

        var bet=Game.New(106,"blade",null,null,false);bet.Player.Gold=50;
        var quote=Game.SideBetQuote(bet,Side.Heads);
        Check(quote!=null&&Game.PlaceSideBet(bet,Side.Heads)&&bet.Player.Gold==50-quote.Stake,"side bet placement");

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
        CheckSettingsPersistence();
        FeatureParityTests.Run();
        Console.WriteLine("latest: focused gameplay checks passed");
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
