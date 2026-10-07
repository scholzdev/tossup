using System.Linq;
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
        var stamp=Game.NewSandbox(new SandboxConfig{Coins=new List<string>{"normal","sword"},Seed=7});
        Check(!Game.StampNormal(stamp,stamp.Coins[1].Uid,"gilded")&&!Game.StampNormal(stamp,stamp.Coins[0].Uid,"sword")&&Game.StampNormal(stamp,stamp.Coins[0].Uid,"slug")&&stamp.Coins[0].Id=="slug","StampNormal turns Normal into a colorless coin only");
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
            var characterRun=Game.New(900+Content.CharacterOrder.IndexOf(characterId),characterId,null,definitions);
            Check(loadout.Count>0&&loadout.Count<=Game.StartMax&&characterRun.Coins.Count==Game.PouchSize,"valid default set starts a full pouch for "+characterId);
            Check(Content.Characters[characterId].Deck.Count==3&&!Content.Characters[characterId].Deck.Exists(c=>c.Id=="normal"),"the default set is the three signature coins of "+characterId);
            Check(Content.Characters[characterId].Perks.Count==2,"two visible perks are defined for "+characterId);
        }
        var stackedProfile = Profile.New();
        var mixedCommons = new List<string> { "normal", "sword", "dagger" };
        Check(Profile.CanAdd(stackedProfile, "blade", mixedCommons, "normal", 5, Game.MaxCopies),
            "different common coins do not consume Normal's copy limit");
        Check(Profile.LimitedSet(new List<string> { "normal", "normal", "normal", "sword", "dagger" }, 5)
                .Count == 5, "loadout repair keeps distinct common coins");
        Check(Game.New(901, "blade", null,
                new List<CoinDef> { CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Sword, CoinCatalog.Dagger })
                .Coins.Count == Game.PouchSize, "run validation accepts stacked copies of distinct common coins");
        Check(!Profile.CanAdd(stackedProfile, "blade", new List<string> { "normal", "normal", "normal" }, "normal", 5, Game.MaxCopies),
            "a fourth copy of one common coin remains blocked");
        var tinkerer=Game.New(911,"tinkerer");
        Check(tinkerer.Player.Energy==tinkerer.Player.MaxEnergy+1,"Tinkerer starts every round with bonus energy");
        tinkerer.ShopOffers=new List<CoinDef>{Content.Coins["normal"]};
        Check(Game.CoinOfferCost(tinkerer,0)==Content.Coins["normal"].Cost-2,"Tinkerer discounts shop coin offers");
        var reflip=Game.New(913,"blade");
        Check(!Game.Reroll(reflip),"no re-flip without a pending result");
        Check(Game.Flip(reflip,reflip.Encounter.Hand[0])&&reflip.Pending!=null,"a coin flips for the re-flip check");
        double reflipEnergy=reflip.Player.Energy;
        Check(Game.Reroll(reflip)&&Near(reflip.Player.Energy,reflipEnergy-1)&&reflip.Pending.Rerolled,"a re-flip costs 1 energy");
        Check(!Game.Reroll(reflip)&&Near(reflip.Player.Energy,reflipEnergy-1),"a coin can be re-flipped only once");
        Game.Resolve(reflip);
        Check(Game.Flip(reflip,reflip.Encounter.Hand[0]),"the next coin flips");
        reflip.Player.Energy=0;
        Check(!Game.Reroll(reflip)&&!reflip.Pending.Rerolled,"no re-flip at 0 energy");
        var naturalist=Game.New(912,"naturalist");
        Check(naturalist.Encounter.Hand.Count==Game.HandSize+1&&Game.CharacterPerkValue(naturalist,CharacterPerkType.TailsGold)==1,"Naturalist gains tails gold and a bigger first hand");
        var naturalistCoin=naturalist.Coins.Find(c=>c.Id=="normal");
        naturalist.Pending=new FlipState{Uid=naturalistCoin.Uid,CoinId=naturalistCoin.Id,Raw=Side.Tails,Result=Side.Tails};
        double naturalistGold=naturalist.Player.Gold;
        Check(Game.Resolve(naturalist)&&naturalist.Player.Gold==naturalistGold+1,"Naturalist earns perk gold when Tails resolves");
        var conductor=Game.New(913,"conductor");
        Check(Near(conductor.Encounter.ComboStep,Game.ComboStep+.1)&&conductor.Encounter.Shield==1,"Conductor improves combo growth and starts shielded");
        Check(Content.Characters["trader"].Pool.Contains(CoinCatalog.SafePort),"Safe Port is available to the Trader");
        Check(Content.ItemOrder.Count==11&&Content.Items.Count==11,"all 11 chips are registered");
        Check(Game.Route.Count==8&&Game.Route[7].Boss,"eight-stage route ends at The House");
        Check(Game.Stakes.Count==8&&Game.Modifiers.Count==8,"stakes and modifiers are complete");
        Check(Game.Encounters.Count==6&&Game.AugmentDefs.Count==7,"run systems are complete");
        Check(EnemyCatalog.Ordered.FindAll(d=>d.Boss).Count==1&&EnemyCatalog.Ordered.FindAll(d=>d.Elite).Count==2&&EnemyCatalog.Ordered.FindAll(d=>!d.Boss&&!d.Elite).Count==6,"six regular enemies, two elites and the boss");
        foreach(var enemy in EnemyCatalog.Ordered)Check(enemy.Pouch.Count>0&&enemy.Pouch.TrueForAll(Content.Coins.ContainsKey)&&enemy.Draw>=4,"enemy pouch is made of known coins: "+enemy.Id);
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

        var stake=Game.New(102,"blade",null,null,8);
        Check(Near(Game.Rule(stake,"enemy_round",0),3)&&Near(Game.Rule(stake,"hand_size",5),4)&&stake.Encounter.HandSize==4,"cumulative stake rules");

        var pity=Game.New(107,"blade");var pc=pity.Coins.Find(c=>c.Id=="normal");
        double pityBase=Game.Probability(pity,pc);
        foreach(var side in new[]{Side.Tails,Side.Tails}){pity.Pending=new FlipState{Uid=pc.Uid,CoinId=pc.Id,Raw=side,Result=side};Game.Resolve(pity);}
        Check(Near(Game.Probability(pity,pc),pityBase+.10),"two Tails raise the shown Heads odds by 0.10");
        pity.Pending=new FlipState{Uid=pc.Uid,CoinId=pc.Id,Raw=Side.Heads,Result=Side.Heads};Game.Resolve(pity);
        Check(Near(Game.Probability(pity,pc),pityBase),"Heads resets the pity bonus");

        var tied=Game.New(103,"trader",null,new List<CoinDef>{CoinCatalog.Loaded});
        var loaded=tied.Coins.Find(c=>c.Id=="loaded");
        Check(loaded!=null&&Near(Content.Coins["loaded"].TieProbability,.23)&&Game.TieEffects("loaded").Count==2,"Edge outcome data");

        var upgraded=Game.New(104,"blade");
        var normal=upgraded.Coins.Find(c=>c.Id=="normal");
        Check(Near(Game.Probability(upgraded,normal),.65),"base Heads odds are unchanged by removed upgrades");

        var augment=Game.New(107,"blade");augment.Phase=Phase.Shop;augment.EncounterIndex=2;
        Check(Game.OfferAugment(augment,3)&&augment.Phase==Phase.Augment&&augment.AugmentOptions.Count==3,"augment offer phase");
        string augmentId=augment.AugmentOptions[0];Check(Game.ChooseAugment(augment,augmentId),"augment selection");
        if(augment.AugmentPending!=null){var choices=Game.AugmentChoices(augment);Check(choices.Count>0&&Game.ChooseAugmentOption(augment,choices[0].Key),"augment follow-up choice");}
        Check(augment.Augments.Contains(augmentId),"augment is owned");

        var flow=Game.New(108,"blade");
        while(flow.Phase!=Phase.Victory)
        {
            Check(flow.Phase==Phase.Encounter,"route enters an encounter");flow.Encounter.Cleared=true;flow.Pending=null;
            Check(Game.EndLevel(flow),"cleared level closes");if(flow.Phase==Phase.Victory)break;
            Check(Game.NextEncounter(flow),"route advances");
            if(flow.Phase==Phase.Augment){string id=flow.AugmentOptions[0];Check(Game.ChooseAugment(flow,id),"route augment chosen");if(flow.AugmentPending!=null){var choices=Game.AugmentChoices(flow);Check(choices.Count>0&&Game.ChooseAugmentOption(flow,choices[0].Key),"route augment option chosen");}}
        }
        Check(flow.EncounterIndex==8,"route reaches the eighth-stage boss");

        var augmentView=Game.New(110,"blade");augmentView.Phase=Phase.Shop;augmentView.EncounterIndex=2;Game.OfferAugment(augmentView,3);Ui.Game=augmentView;AppCore.Draw();Ui.Game=null;

        var profile=Profile.New();Check(Profile.MaxStake(profile,"blade")==1,"new profile starts on stage 1");
        Check(Profile.RecordStakeWin(profile,"blade",1)==2&&Profile.MaxStake(profile,"blade")==2,"winning unlocks next stake");
        string json=Profile.Encode(profile);var restored=Profile.Decode(json);
        Check(Profile.MaxStake(restored,"blade")==2,"stake progression survives JSON");
        CheckCoinMastery();
        CheckMasteryCoverage();
        CheckMap();
        CheckSettingsPersistence();
        CheckTokenUnlock();
        FeatureParityTests.Run();
        CheckLuckReport();
        CheckPouchFight();
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
        var loadedRun = Game.New(1200, "trader", null, new List<CoinDef> { CoinCatalog.Loaded });
        loadedRun.MasteryProfile = loadedProfile;
        var loadedCoin = loadedRun.Coins[0];
        Hooks.Bind(loadedRun, loadedCoin);
        Check(Near(Game.Probability(loadedRun, loadedCoin), .67), "Loaded mastery adds eight points of Heads chance");
        loadedRun.Pending = new FlipState { Uid = loadedCoin.Uid, CoinId = loadedCoin.Id, Raw = Side.Heads, Result = Side.Heads };
        double loadedGold = loadedRun.Player.Gold;
        Check(Game.Resolve(loadedRun) && loadedRun.Player.Gold == loadedGold + 7,
            "Loaded mastery adds three gold to its Heads payout");

        var run = Game.New(1201, "blade", null, new List<CoinDef> { normal });
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
            new List<CoinDef> { CoinCatalog.Whetstone, CoinCatalog.Sword });
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
            new List<CoinDef> { CoinCatalog.Whetstone, CoinCatalog.Sword });
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
        var fuseRun = Game.New(1203, "blade", new List<string> { "fuse" }, new List<CoinDef> { CoinCatalog.Fuse });
        fuseRun.MasteryProfile = fuseProfile;
        var fuse = fuseRun.Coins.Find(c => c.Id == "fuse");
        fuse.Charge = 10;
        Hooks.Bind(fuseRun, fuse);
        fuseRun.Pending = new FlipState { Uid = fuse.Uid, CoinId = fuse.Id, Raw = Side.Heads, Result = Side.Heads };
        double energy = fuseRun.Player.Energy;
        Check(Game.Resolve(fuseRun) && fuseRun.Player.Energy == energy + 1 && fuse.Charge == 0,
            "Fuse level two adds energy when ten charge is spent");
        Profile.AddMastery(fuseProfile, CoinCatalog.Fuse, 200);
        var masteredFuseRun = Game.New(1205, "blade", new List<string> { "fuse" }, new List<CoinDef> { CoinCatalog.Fuse });
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
                var game = Game.New(seed++, "blade", null, null, sandbox: sandbox);
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

    // The pouch loop: five rounds, a hand of five that refills, a pouch that reshuffles, a duel won on points.
    static void CheckPouchFight()
    {
        RuntimeMode.Configure(false,false);
        var g=Game.New(1500,"blade");var e=g.Encounter;
        Check(g.Coins.Count==Game.PouchSize&&e.Hand.Count==Game.HandSize&&e.Pouch.Count==Game.PouchSize-Game.HandSize&&e.Round==1&&e.Discard.Count==0,"a fight starts with five coins in hand and the rest of the pouch");
        Check(e.EnemyId!=null&&Game.EnemyOf(g).Pouch.Count>0&&e.EnemyPouch.Count==Game.EnemyOf(g).Pouch.Count,"every fight has an enemy with its own fixed pouch");
        int last=e.Hand[4],first=e.Hand[0];
        Check(Game.Flip(g,last)&&Game.Resolve(g)&&Game.Flip(g,first)&&Game.Resolve(g)&&e.Flips==2&&e.Hand.Count==3&&e.Discard.Count==2,"any hand coins flip, in any order, without a limit");
        Check(!Game.Flip(g,last),"a flipped coin has left the hand");
        var kept=new List<int>(e.Hand);
        Check(Game.EndRound(g)&&e.Round==2&&e.Hand.Count==Game.HandSize&&e.Hand.GetRange(0,3).SequenceEqual(kept)&&e.EnemyFlips.Count==e.EnemyDraw,"ending the round keeps unflipped coins, refills the hand to five and lets the enemy flip");
        Check(e.EnemyDiscard.Count==e.EnemyDraw&&Near(e.EnemyScore,e.EnemyFlips.Sum(f=>f.Points)+e.EnemyRoundPoints),"the enemy scores the printed points of its coins");
        var back=RunSave.Decode(RunSave.Encode(g));
        Check(back!=null&&back.Encounter.Round==2&&back.Encounter.Hand.SequenceEqual(e.Hand)&&back.Encounter.Pouch.SequenceEqual(e.Pouch)&&back.Encounter.Discard.SequenceEqual(e.Discard)&&
            back.Encounter.EnemyPouch.SequenceEqual(e.EnemyPouch)&&Near(back.Encounter.Scored,e.Scored)&&Near(back.Encounter.EnemyScore,e.EnemyScore)&&back.RngState==g.RngState,"a fight survives a save between flips");
        Check(e.RoundScores.Count==1&&Near(e.RoundScores[0],e.Scored)&&Near(e.EnemyRoundScores[0],e.EnemyScore)&&e.RoundLog.Count==0&&Near(e.RoundStartScored,e.Scored),"the scoreboard keeps per-round points and each round starts a fresh log");
        // the top stage: the stage's per-round points ADD to an enemy's own trait (so a +1 trait enemy gains 4), and the hand holds 4 coins
        var top=Game.New(1501,"blade",null,null,Game.Stakes.Count);var te=top.Encounter;var tdef=Game.EnemyOf(top);
        int wide=te.Modifier=="wide_hand"?1:0;Check(te.HandSize==4+wide&&te.Hand.Count==4+wide,"the stage rule 'hand holds 4 coins' is why a hand has 4 coins");
        Check(Near(te.EnemyRoundPoints,tdef.RoundPoints+3),"the enemy's round points are its trait plus the stage's +3");
        for(int round=1;round<=3;round++)
        {
            Game.EndRound(top);
            Check(te.EnemyRoundScores.Count==round&&Near(te.EnemyRoundScores[round-1],te.EnemyFlips.Sum(f=>f.Points)+te.EnemyRoundPoints),"a round's enemy points are its flips plus its round points, counted once");
        }
        Check(Near(te.EnemyScore,te.EnemyRoundScores.Sum()),"the enemy total is the sum of its rounds");
        // an empty pouch is refilled with the shuffled discard pile
        e.Discard.AddRange(e.Pouch);e.Pouch.Clear();
        foreach(int uid in e.Hand.ToList()){g.Player.Energy=99;Check(Game.Flip(g,uid)&&Game.Resolve(g),"flip for the reshuffle check");}
        int total=e.Hand.Count+e.Pouch.Count+e.Discard.Count;
        Check(total==g.Coins.Count&&e.Hand.Count==0,"every coin is in exactly one place");
        Game.EndRound(g);
        Check(e.Hand.Count==Game.HandSize&&e.Discard.Count==0&&e.Hand.Count+e.Pouch.Count==g.Coins.Count,"an empty pouch is refilled from the discard pile");
        // an enemy penalty is points for the enemy
        double enemy=e.EnemyScore,mine=e.Scored;Game.ApplyEffect(g,null,Effect.Penalty(3));
        Check(Near(e.EnemyScore,enemy+3)&&Near(e.Scored,mine),"a Penalty effect scores for the enemy");
        // five rounds exactly; the player wins only with more points
        foreach(var (mineStart,enemyStart,wins) in new[]{(5.0,5.0,false),(6.0,5.0,true),(4.0,5.0,false)})
        {
            var f=Game.New(1501,"blade");var fe=f.Encounter;fe.EnemyDraw=0;fe.EnemyRoundPoints=0;fe.Scored=mineStart;fe.EnemyScore=enemyStart;
            double gold=f.Player.Gold;int rounds=0;
            while(f.Phase==Phase.Encounter&&!fe.Cleared&&rounds<10){Check(fe.Round==rounds+1,"round counter follows the rounds");Check(Game.EndRound(f),"a round ends");rounds++;}
            Check(rounds==Game.Rounds,"a fight lasts exactly five rounds");
            Check(wins==fe.Cleared&&wins==(f.Phase==Phase.Encounter)&&(wins||f.Phase==Phase.GameOver),"the player wins the fight only with more points than the enemy ("+mineStart+" v "+enemyStart+")");
            Check(wins==(f.Player.Gold>gold)&&f.Cleared==(wins?1:0),"a win pays the level payout and counts the fight");
            Check(!Game.EndRound(f)&&!Game.Flip(f,f.Coins[0].Uid),"nothing flips once the fight is over");
        }
        var rule=Game.New(1502,"blade");rule.Encounter.Scored=rule.Encounter.EnemyScore+1000;
        for(int i=0;i<Game.Rounds;i++)Game.EndRound(rule);
        Check(rule.Encounter.Cleared&&Game.EndLevel(rule)&&rule.Phase==Phase.Shop,"a won fight leads to the shop on a linear run");
    }
    static void CheckLuckReport()
    {
        var g=Game.NewSandbox(new SandboxConfig{Coins=new List<string>{"normal","normal"},Seed=6,Gold=50,Energy=99});
        double p=Game.Probability(g,g.Coins[0]);
        Check(Game.Flip(g,g.Coins[0].Uid)&&Game.Resolve(g),"luck fixture flips");
        Check(g.RunFlips==1&&g.RunHeads<=1&&Near(g.RunExpectedHeads,p),"luck counters advance on a flip");
        g.Phase=Phase.Shop;g.Pending=null;
        var back=RunSave.Decode(RunSave.Encode(g));
        Check(back!=null&&back.RunFlips==1&&back.RunHeads==g.RunHeads&&Near(back.RunExpectedHeads,p),"luck counters survive a run save");
    }
    static string MapKey(GameState g)=>g.MapAt+"|"+g.MapPrompt+"|"+string.Join(";",g.Map.ConvertAll(n=>n.Row+","+n.Lane+","+n.Kind+","+string.Join("/",n.Next)));
    static GameState MapRun(double seed)=>Game.New(seed,"blade",null,null,1,false,null,map:true);
    static void CheckMapShape(GameState g,string why)
    {
        var map=g.Map;int boss=map.Count-1;
        Check(map[boss].Kind==NodeKind.Boss&&map[boss].Row==Game.MapRows&&map[boss].Next.Count==0,"the last node is the boss "+why);
        var reached=new HashSet<int>();var todo=new Stack<int>();
        for(int i=0;i<map.Count;i++)if(map[i].Row==0){Check(map[i].Kind==NodeKind.Table,"row 0 is all tables "+why);reached.Add(i);todo.Push(i);}
        Check(reached.Count==Game.MapStarts,"three starting nodes "+why);
        var early=new HashSet<int>();
        for(int i=0;i<map.Count;i++)if(map[i].Row<Game.MapSplitRow)
            Check(map[i].Next.Count==1&&early.Add(map[i].Next[0]),"the starting paths neither branch nor meet before the split row "+why);
        while(todo.Count>0)foreach(int next in map[todo.Pop()].Next)if(reached.Add(next))todo.Push(next);
        Check(reached.Count==map.Count,"every node is reachable from row 0 "+why);
        for(int i=0;i<boss;i++)
        {
            var n=map[i];
            Check(n.Next.Count>0,"every node leads on "+why);
            if(n.Row==Game.MapRows-1){Check(n.Kind==NodeKind.Mint&&n.Next.Count==1&&n.Next[0]==boss,"the row before the boss is a mint that leads to it "+why);}
            Check(n.Kind!=NodeKind.Boss&&(n.Kind!=NodeKind.Elite||n.Row>=3),"no elite before row 3 "+why);
            foreach(int next in n.Next)
            {
                Check(map[next].Row==n.Row+1&&Math.Abs(map[next].Lane-n.Lane)<=1||map[next].Kind==NodeKind.Boss,"edges climb one row, at most one lane "+why);
                Check(!(n.Kind==NodeKind.Shop&&map[next].Kind==NodeKind.Shop),"no shop right after a shop "+why);
                foreach(var other in map)if(other.Row==n.Row&&other.Lane>n.Lane&&map[next].Kind!=NodeKind.Boss)foreach(int o in other.Next)
                    Check(!(map[o].Lane<map[next].Lane),"edges do not cross "+why);
            }
        }
    }
    static void CheckMap()
    {
        RuntimeMode.Configure(false,false);
        var a=MapRun(31);var b=MapRun(31);
        Check(a.Phase==Phase.Map&&a.Encounter==null&&a.MapAt==null&&a.Map!=null,"a map run starts on the map");
        Check(MapKey(a)==MapKey(b),"the same seed gives the same map");
        bool differs=false;for(int seed=1;seed<=200;seed++){var g=MapRun(seed);CheckMapShape(g,"(seed "+seed+")");if(MapKey(g)!=MapKey(a))differs=true;}
        Check(differs,"different seeds give different maps");
        Check(!Game.ChooseNode(a,a.Map.FindIndex(n=>n.Row==1)),"a node outside row 0 cannot be the first choice");
        var stamp=MapRun(2);stamp.MapPrompt="mint";var plain=stamp.Coins.Find(c=>c.Id=="normal");var other=stamp.Coins.Find(c=>c.Id!="normal");
        Check(!Game.MapChoose(stamp,"stamp",other.Uid)&&Game.MapChoose(stamp,"stamp",plain.Uid)&&plain.Id=="gilded"&&stamp.MapPrompt==null,"the mint stamps a Normal into Gilded, nothing else");
        // walk a whole run: every node kind resolves and returns to the map; the map survives a save at every stop
        int fights=0,elites=0;var visited=new HashSet<NodeKind>();
        for(int seed=1;seed<=60;seed++)
        {
            var g=MapRun(seed);
            for(int guard=0;guard<40&&g.Phase!=Phase.Victory;guard++)
            {
                Check(g.Phase==Phase.Map,"every node returns to the map (seed "+seed+", phase "+g.Phase+")");
                var back=RunSave.Decode(RunSave.Encode(g));
                Check(back!=null&&MapKey(back)==MapKey(g)&&back.Phase==Phase.Map,"the map round-trips through a save");
                var options=new List<int>();for(int i=0;i<g.Map.Count;i++)if(Game.MapReachable(g,i))options.Add(i);
                Check(options.Count>0,"the map always offers a next node");
                if(g.MapAt.HasValue)for(int i=0;i<g.Map.Count;i++)Check(Game.MapReachable(g,i)==g.Map[g.MapAt.Value].Next.Contains(i),"only the next row's linked nodes are reachable");
                int pick=options[(int)((seed+guard)%options.Count)];var kind=g.Map[pick].Kind;visited.Add(kind);string shown=Game.NodeEnemy(g,pick);
                Check(Game.ChooseNode(g,pick)&&g.MapAt==pick,"a reachable node can be chosen");
                Check(!Game.ChooseNode(g,pick),"a second choice waits for the node to finish");
                if(kind==NodeKind.Table||kind==NodeKind.Elite||kind==NodeKind.Boss)
                {
                    Check(g.Phase==Phase.Encounter&&g.Encounter.Boss==(kind==NodeKind.Boss)&&(kind!=NodeKind.Elite||g.Encounter.Name.StartsWith("Elite ")&&g.Encounter.Modifier!=null),"fight nodes start the right encounter");
                    Check(shown!=null&&g.Encounter.EnemyId==shown,"the map shows the enemy you fight");
                    int relics=g.Relics.Count;fights++;if(kind==NodeKind.Elite)elites++;
                    g.Encounter.Cleared=true;g.Cleared++;
                    Check(Game.EndLevel(g),"a cleared fight can be left");
                    Check(kind==NodeKind.Boss?g.Phase==Phase.Victory:g.Phase==Phase.Map,"a fight ends on the map, the boss in victory");
                    if(kind==NodeKind.Elite)Check(g.Relics.Count==relics+1||g.Relics.Count==Content.Relics.Count,"an elite drops a relic");
                }
                else if(kind==NodeKind.Shop){Check(g.Phase==Phase.Shop,"a shop node opens the shop");Check(Game.LeaveShop(g)&&g.Phase==Phase.Map,"leaving the shop returns to the map");}
                else if(kind==NodeKind.Altar)
                {
                    if(g.Phase==Phase.Augment)
                    {
                        Check(RunSave.Decode(RunSave.Encode(g))!=null,"an altar offer can be saved");
                        Check(Game.ChooseAugment(g,g.AugmentOptions[0]),"an altar offers an augment");
                        if(g.AugmentPending!=null)Check(Game.ChooseAugmentOption(g,Game.AugmentChoices(g)[0].Key),"an altar augment can finish its choice");
                    }
                    Check(g.Phase==Phase.Map,"an altar returns to the map");
                }
                else
                {
                    Check(g.Phase==Phase.Map&&g.MapPrompt!=null&&!Game.MapReachable(g,pick),"mint and back room wait for a choice");
                    var saved=RunSave.Decode(RunSave.Encode(g));Check(saved!=null&&saved.MapPrompt==g.MapPrompt,"a pending mint/back room round-trips");
                    double gold=g.Player.Gold;
                    Check(Game.MapChoose(g,"gold")&&g.MapPrompt==null&&g.Player.Gold>gold,"mint and back room pay gold");
                }
            }
            Check(g.Phase==Phase.Victory,"a walk through the map ends at the boss (seed "+seed+")");
        }
        Check(fights>0&&elites>0&&visited.Count>=6,"the walks covered fights, elites and every stop kind");
        // mint and back room options
        var m=MapRun(5);m.Phase=Phase.Map;m.MapPrompt="mint";int deck=m.Coins.Count,uid=m.Coins[0].Uid;
        Check(!Game.MapChoose(m,"item")&&Game.MapChoose(m,"remove",uid)&&m.Coins.Count==deck-1,"the mint melts a coin for free");
        m.MapPrompt="mint";double maxEnergy=m.Player.MaxEnergy;Check(Game.MapChoose(m,"energy")&&m.Player.MaxEnergy==maxEnergy+1,"the mint can give max energy");
        m.MapPrompt="backroom";m.Items.Clear();Check(Game.MapChoose(m,"item")&&m.Items.Count==1,"the back room can give a chip");
        m.MapPrompt="backroom";Check(Game.MapChoose(m,"edge")&&m.NextFightEdge<0&&RunSave.Decode(RunSave.Encode(m))!=null,"the back room can give a head start in the next fight and still saves");
        // an elite fights an elite enemy under a modifier; the head start applies to the next fight
        var table=MapRun(8);Game.ChooseNode(table,table.Map.FindIndex(n=>n.Row==0));
        var elite=MapRun(8);elite.Map[elite.Map.FindIndex(n=>n.Row==0)].Kind=NodeKind.Elite;Game.ChooseNode(elite,elite.Map.FindIndex(n=>n.Row==0));
        Check(EnemyCatalog.ById[elite.Encounter.EnemyId].Elite&&!EnemyCatalog.ById[table.Encounter.EnemyId].Elite&&elite.Encounter.Modifier!=null,"an elite meets an elite enemy under a modifier, a table a regular one");
        var edge=MapRun(9);edge.NextFightEdge=-5;Game.ChooseNode(edge,edge.Map.FindIndex(n=>n.Row==0));
        Check(Near(edge.Encounter.Scored,5)&&Near(edge.Encounter.EnemyScore,0)&&Near(edge.NextFightEdge,0),"a head start gives you points at the start of the next fight");
        // old linear saves and runs still work
        var linear=Game.New(77,"blade");
        Check(linear.Map==null&&linear.Phase==Phase.Encounter&&RunSave.Decode(RunSave.Encode(linear))!=null,"runs without a map stay linear");
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

    static void CheckTokenUnlock()
    {
        RuntimeMode.Configure(false, false);
        string savedProfile = Ui.Platform.ReadSave("profile.json");
        var coin = Content.Characters["blade"].Locked.Find(entry => entry.Coin.Rarity == Rarity.Rare).Coin;
        int price = Game.TokenPrice(coin);
        Ui.Profile = Profile.New();
        Ui.Profile.Tokens = price - 1;
        A.UnlockWithTokens("blade", coin);
        Check(!Profile.IsUnlocked(Ui.Profile, "blade", coin.Id) && Ui.Profile.Tokens == price - 1, "unlocking with too few tokens is refused");
        Ui.Profile.Tokens = price + 2;
        A.UnlockWithTokens("blade", coin);
        Check(Profile.IsUnlocked(Ui.Profile, "blade", coin.Id) && Ui.Profile.Tokens == 2, "unlocking spends the token price");
        A.UnlockWithTokens("blade", coin);
        Check(Ui.Profile.Tokens == 2 && Profile.Decode(Ui.Platform.ReadSave("profile.json")).Tokens == 2, "an unlocked coin is not charged twice and the spend is saved");
        Check(Profile.AddToSet(Ui.Profile, "blade", 2, coin.Id, Game.StartMax, Game.MaxCopies), "the token-unlocked coin can go into a set");

        if (savedProfile == null) Ui.Platform.DeleteSave("profile.json");
        else Ui.Platform.WriteSave("profile.json", savedProfile);
        A.LoadProfile();
    }
}
