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
            if(run.RunEncounter!=Game.EncounterById["upgrade"])continue;
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
        g = Scene("reprise");
        int repriseUid = g.Coins[0].Uid;
        Play(g, "reprise", Side.Heads);
        Check(g.Encounter.Returned == 1 && (g.Encounter.Pile.Contains(repriseUid) || g.Dealt?.Uid == repriseUid),
            "Reprise returns itself after its first resolved flip");
        if (g.Dealt?.Uid != repriseUid) Check(Game.Select(g, repriseUid), "Reprise can be selected for its repeat flip");
        Check(Game.Flip(g), "Reprise can be flipped again");
        g.Pending.Result = Side.Heads;
        Check(Game.Resolve(g) && g.Encounter.Returned == 1, "Reprise pays twice but returns only once per level");
        g = Scene("normal", "hammer"); Play(g, "normal", Side.Tails); g.Player.Energy = 0; Game.Select(g, g.Coins[1].Uid);
        Check(Game.CoinsLeft(g) == 1 && Game.CanFlip(g), "last remaining coin can emergency flip");
        Game.Flip(g); Check(Near(g.Player.Gold, 48), "emergency energy costs at most two gold");
        g = Scene("normal", "normal"); Game.AddBuff(g, "swap", 0, 1, true); Play(g, "normal", Side.Tails);
        Check(Near(g.LastResult.Gained.Value, 1), "swapped effect side uses the base coin outcome");
        g = Scene("whetstone", "echo", "normal"); Play(g, "whetstone", Side.Heads); Play(g, "echo", Side.Heads);
        Check(g.LastResult.BaseBuffs.Any(b => b.Id == "whetstone_steel" && b.Target.Type == CoinType.Steel && b.Target.Count == 2), "Echo retains typed coin target and duration");
        g = Scene("megaphone", "echo", "normal"); Play(g, "megaphone", Side.Heads); Play(g, "echo", Side.Heads);
        Check(g.LastResult.BaseBuffs.Any(b => b.Id == "megaphone_double_next" && b.Effect.Type == EffectType.NextMult && b.Target.Count == 2), "Echo retains typed multiplier duration");
        g = Scene("doppelganger", "contrarian", "normal"); g.Augments.Add("type_specialist"); g.AugmentData["type_specialist"] = "chaos";
        Play(g, "doppelganger", Side.Heads); Play(g, "contrarian", Side.Heads);
        Check(g.LastResult.BaseEffects.Where(e => e.Type == EffectType.Score).Select(e => e.Amount).SequenceEqual(new double[] { 4,1,4,1 }), "Chaos includes specialist before duplication");
        g = Scene("counterfeiter", "normal", "bank"); Play(g, "counterfeiter", Side.Heads);
        Play(g, "normal", Side.Heads);
        Check(g.Encounter.Buffs.Exists(b => b.Kind == "greed" && b.Left == 2), "type-targeted buff waits through unrelated coins");
        Play(g, "bank", Side.Heads);
        Check(g.Encounter.Buffs.Exists(b => b.Kind == "greed" && b.Left == 1), "type-targeted buff consumes one matching coin");
        g = Scene("normal", "normal");
        Game.AddBuff(g, new BuffSpec("test_heads_gold", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.Gold(2), OutcomeSide.Heads), true);
        Play(g, "normal", Side.Heads);
        Check(Near(g.Player.Gold, 52) && g.LastResult.BaseEffects.Exists(e => e.Type == EffectType.Gold && e.Amount == 2), "typed buff effects apply on their declared outcome");
        g = Scene("normal", "normal");
        Game.AddBuff(g, new BuffSpec("test_any_side_gold", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.Gold(3)), true);
        Play(g, "normal", Side.Tails);
        Check(Near(g.Player.Gold, 53) && g.LastResult.BaseEffects.Exists(e => e.Type == EffectType.Gold && e.Amount == 3),
            "typed buff effects without an outcome restriction apply on either side");
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
        Check(Profile.Loadout(profile,"blade",5,3).SequenceEqual(new[] { "normal","normal","normal","compost" }), "loadout preserves stacked copies of distinct common coins");
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
        Check(Content.CoinOrder.Count >= 100 && Content.CoinOrder.Select(c=>c.Id).Distinct().Count()==Content.CoinOrder.Count, "catalog keeps all unique coin objects");
        foreach(var definition in Content.CoinOrder)
            Check(ReferenceEquals(definition,Content.Coins[definition.Id]) && definition.GetType()!=typeof(CoinDef), "catalog indexes the concrete definition object");
        var additions = new[] { "parry", "executioner", "blood_price", "omen", "moonwatch", "paradox", "harvest", "broker", "windfall", "spare_coil", "overclock", "seedling", "symbiosis", "crescendo", "counterpoint", "lunge", "feint", "sunder", "grit", "bloodletting", "premonition", "constellation", "fateweaver", "looking_glass", "dividend", "loan_note", "rebate", "arbitrage", "salvage", "caliper", "prototype", "reactor", "rootstock", "mycelium", "thicket", "pollinator", "encore", "drumroll", "syncopation", "finale" };
        foreach(var id in additions)
        {
            Check(Content.Coins.ContainsKey(id), id + " is registered");
            string repositoryRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            Check(System.IO.File.Exists(System.IO.Path.Combine(repositoryRoot, "Assets", "Resources", "coins", id + ".png")), id + " has coin art");
            bool discoverable = false;
            foreach(var character in Content.Characters.Values)
            {
                if(character.Pool.Exists(c=>c.Id==id) || character.Deck.Exists(c=>c.Id==id) || character.Locked.Exists(c=>c.Id==id)) discoverable=true;
            }
            Check(discoverable, id + " is available through a character");
        }
        var moonwatch = Scene("moonwatch");
        var moonCoin = moonwatch.Coins[0];
        moonwatch.Encounter.ElapsedSeconds = 0;
        var moonHeads = Game.GetOdds(moonwatch, moonCoin);
        moonwatch.Encounter.ElapsedSeconds = 2;
        var moonEdge = Game.GetOdds(moonwatch, moonCoin);
        Check(moonHeads.Heads > moonEdge.Heads && moonEdge.Edge > moonHeads.Edge, "Moonwatch shifts odds between Heads and Edge on its clock");
        var overclock = Scene("overclock", "normal");
        int overclockUid = overclock.Coins.Find(c=>c.Id=="overclock").Uid;
        Play(overclock, "overclock", Side.Heads);
        Check(overclock.Encounter.Returned==1 && !overclock.Encounter.Played.Contains(overclockUid), "Overclock returns itself after its first Heads for a second flip");
        var symbiosis = Scene("symbiosis", "compost");
        Play(symbiosis, "symbiosis", Side.Heads);
        Check(symbiosis.Encounter.Buffs.Exists(b=>b.SpecId=="symbiosis" && b.TargetType==CoinType.Fortune && b.Left==1), "Symbiosis queues its buff for the next Fortune coin");
        double symbiosisScored = symbiosis.Encounter.Scored;
        Play(symbiosis, "compost", Side.Heads);
        Check(symbiosis.Encounter.Scored-symbiosisScored>=2, "Symbiosis buff adds points to the next Fortune coin");
        var counterpoint = Scene("normal", "counterpoint");
        Play(counterpoint, "normal", Side.Heads);
        double beforeCounterpoint = counterpoint.Encounter.Scored;
        Play(counterpoint, "counterpoint", Side.Tails);
        Check(counterpoint.Encounter.Scored-beforeCounterpoint==5, "Counterpoint rewards a side change from the preceding result");
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
        game.ShopOffers[0]=CoinCatalog.Normal;
        string encoded=RunSave.Encode(game);
        Check(encoded.Contains("\"Id\":\"normal\"")&&!encoded.Contains("\"Definition\""),"version-1 saves retain coin ID keys");
        var restored=RunSave.Decode(encoded);
        Check(restored!=null&&ReferenceEquals(restored.Coins[0].Definition,CoinCatalog.Normal)&&ReferenceEquals(restored.ShopOffers[0],CoinCatalog.Normal),"resume resolves saved IDs into shared definition objects");
        string legacy = encoded.Replace("\"Id\":\"normal\"", "\"Id\":\"normal\",\"Upgrade\":\"lucky_day\",\"Bonus\":0.1")
            .Replace("\"ShopOffers\":", "\"ShopUpgrades\":[\"lucky_day\"],\"ShopOffers\":");
        Check(legacy != encoded && RunSave.Decode(legacy) != null, "older saves ignore retired coin and shop upgrade fields");
        game.Player.Gold=100;
        Check(Game.Buy(game,0) && game.ShopOffers[0]==null && game.Coins[game.Coins.Count-1].Id=="normal","buying a coin does not attach an upgrade");
        Check(RunSave.Decode(RunSave.Encode(game))!=null,"shop checkpoint remains resumable after buying a coin");

        var allInHeads = Scene("allin");
        Play(allInHeads, "allin", Side.Heads);
        Check(Near(allInHeads.LastResult.Gained.Value, 10), "All-In doubles its score on Heads");
        var allInTails = Scene("allin");
        Play(allInTails, "allin", Side.Tails);
        Check(Near(allInTails.LastResult.Gained.Value, 0), "All-In scores nothing on Tails");
        RuntimeMode.Configure(false,false);
    }
}
