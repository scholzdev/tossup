using System;
using System.Collections.Generic;
using System.Linq;
using Tossup;
using Tossup.UI;

// Scenarios reproduced against main@63a7c55 in docs/parity-audit/evidence.json.
static class AuditRegressionTests
{
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("Lua parity regression: " + message); }
    static bool Near(double a, double b) => Math.Abs(a-b) < 1e-9;
    static GameState Scene(params string[] ids)
    {
        var game = Game.NewSandbox(new SandboxConfig { Coins = new List<string>(ids), Seed = 6, Gold = 50, Energy = 99 });
        game.Encounter.Quota = game.Encounter.MaxQuota = 999;
        return game;
    }
    static void Play(GameState game, string id, string side)
    {
        var coin = game.Coins.Find(c => c.Id == id);
        Check(Game.Select(game, coin.Uid) && Game.Flip(game), "fixture can play " + id);
        game.Pending.Result = side;
        Check(Game.Resolve(game), "fixture resolves " + id);
    }
    public static void Run()
    {
        DefinitionObjects();
        RuntimeMode.Configure(false,false);
        int upgradeRewards=0;
        for(int seed=1;seed<=100;seed++)
        {
            var run=Game.New(seed*100003,"trader");
            if(run.RunEncounterId!="upgrade")continue;
            upgradeRewards++;
            Check(Content.Characters[run.CharacterId].Pool.Contains(run.Coins[run.Coins.Count-1].Definition),"Upgrade encounter reward belongs to the usable pool");
        }
        Check(upgradeRewards>0,"Upgrade reward fixture exercises the encounter");
        var quotaScene=new SandboxConfig {Coins=new List<string>{"capacitor"},Seed=6,Energy=10};
        quotaScene.Odds["capacitor"]=new SandboxOdds {Heads=1,Tie=0};
        double chargedQuota=Game.NewSandbox(quotaScene).Encounter.Quota;
        quotaScene.Energy=1;
        Check(chargedQuota>Game.NewSandbox(quotaScene).Encounter.Quota,"initial energy affects capacitor quota estimate");
        quotaScene.Energy=10;quotaScene.Odds["capacitor"].Heads=0;
        Check(chargedQuota>Game.NewSandbox(quotaScene).Encounter.Quota,"sandbox override odds affect initial quota");
        var g = Scene("normal", "normal", "normal");
        Game.PlaceSideBet(g, Side.Heads); Play(g, "normal", Side.Heads); Play(g, "normal", Side.Heads);
        Check(Near(g.Player.Gold, 53), "side bets settle once");
        g = Scene("normal", "hammer"); Play(g, "normal", Side.Tails); g.Player.Energy = 0; Game.Select(g, g.Coins[1].Uid);
        Check(Game.CoinsLeft(g) == 1 && Game.CanFlip(g), "last remaining coin can emergency flip");
        Game.Flip(g); Check(Near(g.Player.Gold, 48), "emergency energy costs at most two gold");
        g = Scene("normal", "normal"); g.Coins[0].Upgrade = UpgradeCatalog.LuckyDay; Game.AddBuff(g, "swap", 0, 1, true); Play(g, "normal", Side.Tails);
        Check(Near(g.LastResult.Gained.Value, 2), "upgrades follow swapped effect side");
        g = Scene("whetstone", "echo", "normal"); Play(g, "whetstone", Side.Heads); Play(g, "echo", Side.Heads);
        Check(g.LastResult.BaseEffects.Any(e => e.Kind == CoinType.Steel && e.Coins == 2), "Echo retains coin type and duration");
        g = Scene("megaphone", "echo", "normal"); Play(g, "megaphone", Side.Heads); Play(g, "echo", Side.Heads);
        Check(g.LastResult.BaseEffects.Any(e => e.Type == EffectType.NextMult && e.Coins == 2), "Echo retains multiplier duration");
        g = Scene("doppelganger", "contrarian", "normal"); g.Augments.Add("type_specialist"); g.AugmentData["type_specialist"] = "chaos";
        Play(g, "doppelganger", Side.Heads); Play(g, "contrarian", Side.Heads);
        Check(g.LastResult.BaseEffects.Where(e => e.Type == EffectType.Score).Select(e => e.Amount).SequenceEqual(new double[] { 4,1,4,1 }), "Chaos includes specialist before duplication");
        g = Scene("normal", "normal"); g.Relics.Add("clock"); Relics.Bind(g); g.Encounter.Flips = 9; g.Encounter.Inverts = true; Game.Flip(g);
        Check(g.Pending.Result == Side.Heads, "Broken Clock protects tenth Heads from stage inversion");
        g = Scene("cash_out", "normal"); Play(g, "cash_out", Side.Heads);
        Check(g.Encounter.ComboLen == 0 && g.Encounter.ComboSide == null, "Cash Out resets zero-value pots");
        g = Scene("normal", "normal"); g.Encounter.Contract = new ContractState { Id = "quick_clear" }; g.Phase = Phase.Shop;
        Check(Near(Game.Probability(g,g.Coins[0]), .65), "shop ignores previous contract odds");
        g = Scene("normal", "normal", "normal"); g.Encounter.Contract = new ContractState { Id = "amazon_prime" }; g.Encounter.Quota = 1; Play(g,"normal",Side.Heads);
        Check(g.Coins.Count == 3 && g.Encounter.Contract.Result == "MISSED", "Amazon failure waits for level exit");
        Check(Game.EndLevel(g) && g.Coins.Count == 1, "Amazon penalty applies on exit");
        Check(RunSave.Decode(RunSave.Encode(g)) != null, "Amazon removed coins do not invalidate shop resume");
        g = Scene("mimic", "lucky"); Game.AddBuff(g, "chaos", 0, 1, true); Play(g, "mimic", Side.Heads);
        var live = g.Encounter.Queue.Concat(g.Encounter.Pile).ToArray();
        Check(g.Encounter.Returned == 1 && live.Length == live.Distinct().Count(), "copied return effects cannot duplicate a live UID");
        g = Scene("blood"); g.Sandbox.Odds["blood"] = new SandboxOdds { Heads=.10, Tie=.80 }; Game.ApplyEffect(g,g.Coins[0],new Effect(EffectType.AllOdds,.07));
        Check(Near(Game.Probability(g,g.Coins[0]),.17), "sandbox override combines with buffs");
        g = Scene("blood","blood"); Game.OpenSandboxShop(g);
        Check(g.ShopOffers.SequenceEqual(new CoinDef[] { CoinCatalog.Blood }), "sandbox shops use restricted pool");
        g = Scene("normal","normal","normal"); g.Encounter.Quota = 1; Play(g,"normal",Side.Heads); Game.EndLevel(g);
        Check(Game.Remove(g,g.Coins[0].Uid) && RunSave.Decode(RunSave.Encode(g)) != null, "shop removal remains resumable");
        var profile = Profile.Decode("{\"tokens\":-3,\"stakes\":{\"blade\":99},\"options\":{\"volumeMaster\":200,\"language\":\"xx\"}}");
        Check(profile.Tokens == 0 && Profile.MaxStake(profile,"blade") == 8 && profile.Options.VolumeMaster == 100 && profile.Options.Language == "en", "profile values are sanitized");
        profile = Profile.New(); Profile.Grant(profile,"blade","compost"); profile.Sets["blade"] = new List<CoinSet> { new CoinSet { Coins = new List<string> { "normal","normal","normal","compost" } } };
        Check(Profile.Loadout(profile,"blade",5,3).SequenceEqual(new[] { "normal","normal","compost" }), "loadout repairs preserve later picks");
        RuntimeMode.Configure(false,false);
        g = Game.New(6,"blade",null,null,true); Check(RunSave.Decode(RunSave.Encode(g)) != null, "untouched level can resume");
        Game.MulliganDone(g); Game.Flip(g); Check(!RunSave.IsSafePoint(g), "pending flips cannot overwrite restart checkpoint");
        g = Scene("normal","normal"); g.Items.AddRange(new[] { "energy_drink","shortcut","safety_net" }); Ui.Game = g; Ui.Holding = false; Ui.FlipAnimation = null;
        AppCore.KeyPressed("1"); Check(!g.Items.Contains("energy_drink") && g.Items.Contains("shortcut"), "key 1 uses first chip");
        Ui.Game = null; RuntimeMode.Configure(false,false);
        string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        string fixture = System.IO.Path.Combine(root,"Tools","uitest","fixtures");
        var importedProfile = LegacySave.ProfileFromLua(System.IO.File.ReadAllText(System.IO.Path.Combine(fixture,"profile.lua")));
        Check(importedProfile != null && importedProfile.Tokens == 17 && importedProfile.Options.Language == "de" && Profile.MaxStake(importedProfile,"blade") == 4 && importedProfile.Wins.Contains("blade") && importedProfile.Collected.Contains("blood") && Profile.IsUnlocked(importedProfile,"blade","compost"), "real Lua profile imports progress and options");
        Check(Profile.Sets(importedProfile,"blade")[0].Name == "Münzen", "Lua profile imports Unicode set names");
        var importedRun = LegacySave.RunFromLua(System.IO.File.ReadAllText(System.IO.Path.Combine(fixture,"run.lua")));
        Check(importedRun != null && importedRun.Seed == 6 && importedRun.Stake == 3 && importedRun.Mulligan != null, "real Lua checkpoint imports and validates");
        var importedShop=LegacySave.RunFromLua(System.IO.File.ReadAllText(System.IO.Path.Combine(fixture,"shop.lua")));
        Check(importedShop!=null&&importedShop.Phase==Phase.Shop&&importedShop.Coins.Count==2&&importedShop.Coins.All(c=>ReferenceEquals(c.Definition,Content.Coins[c.Id])),"real Lua shop imports after removal with obsolete UID history");
        var importedAugment=LegacySave.RunFromLua(System.IO.File.ReadAllText(System.IO.Path.Combine(fixture,"augment.lua")));
        Check(importedAugment!=null&&importedAugment.Phase==Phase.Augment&&importedAugment.AugmentPending.Id=="type_specialist"&&Game.ChooseAugmentOption(importedAugment,Game.AugmentChoices(importedAugment)[0].Key),"real Lua pending augment imports and completes its choice");
        Check(LegacySave.ProfileFromLua("return os.execute('invalid')") == null && LegacySave.ProfileFromLua("return {tokens=0,unlocked={},x=function() end}") == null, "Lua import rejects executable input");
        string migrated = System.IO.Path.Combine(System.IO.Path.GetTempPath(),"tossup-legacy-migration-test-" + Guid.NewGuid().ToString("N"));
        Check(LegacySave.ImportDirectory(fixture,migrated), "legacy saves import into an absent Unity profile/run");
        string savedProfile = System.IO.File.ReadAllText(System.IO.Path.Combine(migrated,"profile.json"));
        Check(!LegacySave.ImportDirectory(fixture,migrated) && savedProfile == System.IO.File.ReadAllText(System.IO.Path.Combine(migrated,"profile.json")), "legacy import never overwrites existing Unity progress");
        System.IO.Directory.Delete(migrated,true);
        Console.WriteLine("audit regressions: gameplay, checkpoint/removal, profile, sandbox and chip indexing passed");
    }

    static void DefinitionObjects()
    {
        Check(Content.CoinOrder.Count == 59 && Content.CoinOrder.Select(c=>c.Id).Distinct().Count()==59, "catalog keeps all unique coin objects");
        foreach(var definition in Content.CoinOrder)
            Check(ReferenceEquals(definition,Content.Coins[definition.Id]) && definition.GetType()!=typeof(CoinDef), "catalog indexes the concrete definition object");
        Check(UpgradeCatalog.Ordered.Count==18 && UpgradeCatalog.Ordered.Select(u=>u.Id).Distinct().Count()==18,"all upgrade definitions have unique IDs");
        foreach(var coin in Content.CoinOrder)
            foreach(var upgrade in coin.Upgrades)
                Check(ReferenceEquals(upgrade,UpgradeCatalog.ById[upgrade.Id]),"coins reference the shared upgrade definitions");
        Check(D.CoinOutcomeDescription(CoinCatalog.AllIn, Side.Heads) == "Doubles the score." &&
              D.CoinOutcomeDescription(CoinCatalog.AllIn, Side.Tails) == "Scores nothing.",
            "custom coin outcome descriptions replace the empty-effect placeholder");
        var game=Scene("normal","normal");game.Encounter.Quota=1;
        Check(ReferenceEquals(game.Coins[0].Definition,CoinCatalog.Normal),"owned coin carries its definition object");
        var effect=CoinCatalog.Normal.Heads[0];double original=effect.Amount;
        try
        {
            effect.Amount=7;
            Play(game,"normal",Side.Heads);
            Check(Near(game.LastResult.Gained.Value,7),"editing a definition affects the actual resolving coin");
        }
        finally {effect.Amount=original;}
        Game.EndLevel(game);
        game.Coins[0].Upgrade=UpgradeCatalog.LuckyDay;
        game.ShopOffers[0]=CoinCatalog.Normal;game.ShopUpgrades[0]=UpgradeCatalog.Mathematician;
        string encoded=RunSave.Encode(game);
        Check(encoded.Contains("\"Id\":\"normal\"")&&!encoded.Contains("\"Definition\""),"version-1 saves retain coin ID keys");
        var restored=RunSave.Decode(encoded);
        Check(restored!=null&&ReferenceEquals(restored.Coins[0].Definition,CoinCatalog.Normal)&&ReferenceEquals(restored.ShopOffers[0],CoinCatalog.Normal),"resume resolves saved IDs into shared definition objects");
        Check(encoded.Contains("\"Upgrade\":\"lucky_day\"") && ReferenceEquals(restored.Coins[0].Upgrade,UpgradeCatalog.LuckyDay) && ReferenceEquals(restored.ShopUpgrades[0],UpgradeCatalog.Mathematician),"version-1 saves restore shared owned/shop upgrade objects");
        Check(RunSave.Decode(encoded.Replace("\"lucky_day\"","\"safer_bet\""))==null,"restore rejects an upgrade belonging to another coin");
        game.Player.Gold=100;
        Check(Game.Buy(game,0) && game.ShopOffers[0]==null && game.ShopUpgrades[0]==null && ReferenceEquals(game.Coins[game.Coins.Count-1].Upgrade,UpgradeCatalog.Mathematician),"buying an upgraded offer transfers its definition and clears sold-slot metadata");
        Check(RunSave.Decode(RunSave.Encode(game))!=null,"shop checkpoint remains resumable after buying an upgraded offer");

        var allInHeads = Scene("allin");
        Play(allInHeads, "allin", Side.Heads);
        Check(Near(allInHeads.LastResult.Gained.Value, 10), "All-In doubles its score on Heads");
        var allInTails = Scene("allin");
        Play(allInTails, "allin", Side.Tails);
        Check(Near(allInTails.LastResult.Gained.Value, 0), "All-In scores nothing on Tails");
        RuntimeMode.Configure(false,false);
    }
}
