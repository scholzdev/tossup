using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Tossup;

// Balance tools use the same rules and event bus as the player. Measurements are
// isolated neutral decks; full runs retain encounters, contracts and augments.
static class Simulator
{
    sealed class Measurement
    {
        public double Points, Gold, Penalty, Energy, Value;
        public int Flips;
    }
    static readonly Dictionary<string, Measurement> measured = new();
    static Measurement Measure(string id, int games, int flips)
    {
        Content.Characters["sim"] = new CharacterDef { Id="sim", Name="Sim", Starter=CoinCatalog.Normal, Pool=new List<CoinDef>{CoinCatalog.Normal,CoinCatalog.Sword,CoinCatalog.Dagger,CoinCatalog.Hammer}, Deck=new List<CoinDef>{Content.Coins[id],CoinCatalog.Sword,CoinCatalog.Dagger,CoinCatalog.Normal,CoinCatalog.Normal} };
        var m = new Measurement(); double before=0;
        var handles = new[] {
            Signal.On(GameSignal.CoinFlip, e=> { if(e.Inst.Id==id) before=e.Game.Player.Gold; }),
            Signal.On(GameSignal.EffectApplied, e=> { if(e.Inst.Id!=id)return; switch(e.Effect.Type) {case EffectType.Score:m.Points+=e.Effect.Amount;break;case EffectType.Penalty:m.Penalty+=e.Effect.Amount;break;case EffectType.Energy:m.Energy+=e.Effect.Amount;break;} }),
            Signal.On(GameSignal.CoinResolved, e=> { if(e.Inst.Id==id){m.Gold+=e.Game.Player.Gold-before;m.Flips++;} })
        };
        try
        {
            for(int seed=1;seed<=games;seed++)
            {
                var g=Game.New(seed,"sim",applyRunEncounter:false);g.ContractsEnabled=false;g.Player.Gold=30;
                for(int step=1;step<=flips;step++)
                {
                    g.Encounter.Quota=g.Encounter.MaxQuota=1e9;
                    if(step%15==1){g.Encounter.Doubler=0;g.Encounter.Tails=0;}
                    g.Reshuffle=true;g.Player.Energy=Math.Max(g.Player.Energy,3);
                    if(g.Dealt!=null&&Game.Flip(g))Game.Resolve(g);
                }
            }
        }
        finally {foreach(var h in handles)Signal.Off(h);Content.Characters.Remove("sim");}
        double n=Math.Max(1,m.Flips);m.Points/=n;m.Gold/=n;m.Penalty/=n;m.Energy/=n;
        m.Value=m.Points-m.Penalty+.5*m.Gold+.3*m.Energy-.5*Content.Coins[id].EnergyCost;
        return m;
    }
    static double Value(string id)
    {
        if(!measured.TryGetValue(id,out var m))measured[id]=m=Measure(id,40,100);
        return m.Value;
    }
    static List<string> Unlocks(string character,bool all) => all ? Content.Characters[character].Locked.Select(x=>x.Id).ToList() : null;
    static List<string> BestSet(string character,bool all)
    {
        var ids=Content.Characters[character].Pool.Select(c=>c.Id).Concat(Unlocks(character,all)??new List<string>()).Distinct().OrderByDescending(Value).ThenBy(x=>x,StringComparer.Ordinal);
        var set=new List<string>();var counts=new Dictionary<Rarity,int>();
        void Add(string id) {var rarity=Content.Coins[id].Rarity;counts.TryGetValue(rarity,out int n);while(set.Count<Game.StartMax&&n<Profile.RarityLimit(id)){set.Add(id);counts[rarity]=++n;} }
        foreach(string id in ids)if(id!="normal"&&Value(id)>Value("normal"))Add(id);
        Add("normal");return set;
    }
    static void FinishFlip(GameState g)
    {
        if(!Game.CanFlip(g)&&Game.Discard(g)>0)return;
        if(Game.Flip(g))Game.Resolve(g);
    }
    static void PlayCoin(GameState g,string bot,Dictionary<string,double> values)
    {
        if(bot!="smart"){FinishFlip(g);return;}
        double mean=g.Coins.Average(c=>values[c.Id]);
        var coin=Game.GetCoin(g,g.Dealt.Uid);double v=values[coin.Id];
        bool behind=g.Encounter.Quota>Game.CoinsLeft(g)*mean*.9;
        if(v<mean*.6&&g.Coins.Count-g.Encounter.Discards>1&&Game.Discard(g)>0)return;
        bool Use(string id){int slot=g.Items.IndexOf(id);return slot>=0&&Game.UseItem(g,slot);}
        if(v<mean*.5&&Use("swap"))return;
        double pts=Content.Coins[coin.Id].Heads.Where(e=>e.Type==EffectType.Score).Sum(e=>e.Amount);
        if(behind){if(pts>=3)Use("double_down");if(pts>=2)Use("force_heads");if(g.Dealt!=null&&g.Dealt.Probability<.75)Use("weighted");}
        if(Game.CoinsLeft(g)<=2&&g.Encounter.Quota>0&&g.Encounter.Quota<=2*mean)Use("extra_draw");
        FinishFlip(g);
    }
    static void Shop(GameState g,string bot,Dictionary<string,double> values)
    {
        if(bot=="random"){for(int n=0;n<8;n++)if(!Game.Buy(g,(int)(g.RngState%4)))break;return;}
        while(true)
        {
            if(bot=="greedy"&&g.Coins.Count>=g.Slots&&!Game.BuySlot(g))break;
            int best=-1;double bestValue=double.NegativeInfinity;
            for(int i=0;i<g.ShopOffers.Count;i++)
            {
                string id=g.ShopOffers[i]?.Id;if(id==null||g.Player.Gold<Game.CoinOfferCost(g,i))continue;
                double v=bot=="smart"?values[id]:(Content.Coins[id].Cost);
                if(v>bestValue){best=i;bestValue=v;}
            }
            if(best<0)break;
            if(g.Coins.Count>=g.Slots)
            {
                int price=Game.CoinOfferCost(g,best);
                if(g.Slots<Game.DeckMax&&g.Player.Gold>=Game.SlotPrice(g)+price&&bestValue>.5){if(!Game.BuySlot(g))break;}
                else
                {
                    var worst=g.Coins.OrderBy(c=>values[c.Id]).First();
                    if(bestValue<=values[worst.Id]*1.3+.1||g.Player.Gold<8+price||!Game.Remove(g,worst.Uid))break;
                }
            }
            if(!Game.Buy(g,best))break;
        }
        if(bot!="smart")return;
        if(g.ShopRelic!=null&&g.Player.Gold>=25)Game.BuyRelic(g);
        for(int i=0;i<g.ShopItems.Count;i++)if(new[]{"force_heads","double_down","weighted","extra_draw"}.Contains(g.ShopItems[i]))Game.BuyItem(g,i);
    }
    static GameState Play(int seed,string character,string bot,bool all,bool defaults,int stake)
    {
        // Measure before constructing the run: Game.New rebinds global rule hooks.
        var values=bot=="smart"||!defaults ? Content.CoinOrder.ToDictionary(c=>c.Id,c=>Value(c.Id)) : null;
        var loadout=defaults?null:BestSet(character,all);
        var g=Game.New(seed,character,Unlocks(character,all),loadout?.Select(id=>Content.Coins[id]).ToList(),true,stake);
        for(int guard=0;guard<2000;guard++)
        {
            if(g.Phase==Phase.Victory||g.Phase==Phase.GameOver)return g;
            if(g.Phase==Phase.Contract){Game.SkipContract(g);continue;}
            if(g.Mulligan!=null)
            {
                if(bot=="smart")
                {
                    var hand=g.Mulligan.Hand;double mean=hand.Average(uid=>values[Game.GetCoin(g,uid).Id]);var marked=new List<int>();
                    foreach(int uid in hand)if(hand.Count-marked.Count>3&&values[Game.GetCoin(g,uid).Id]<mean*.6)marked.Add(uid);
                    Game.MulliganDiscard(g,marked);
                }
                Game.MulliganDone(g);continue;
            }
            if(g.Phase==Phase.Augment)
            {
                if(g.AugmentPending!=null){var choices=Game.AugmentChoices(g);if(choices.Count==0)throw new InvalidOperationException("augment has no choices");Game.ChooseAugmentOption(g,choices[0].Key);}
                else Game.ChooseAugment(g,g.AugmentOptions[0]);
                continue;
            }
            if(g.Phase==Phase.Shop){Shop(g,bot,values);Game.LeaveShop(g);continue;}
            if(g.Dealt!=null)PlayCoin(g,bot,values);
            else if(g.Encounter.Cleared)Game.EndLevel(g);
            else if(Game.CanExchange(g))Game.Exchange(g);
            else Game.GiveUp(g);
            if(g.Phase==Phase.Encounter&&g.Encounter.Cleared&&bot!="smart")Game.EndLevel(g);
        }
        throw new InvalidOperationException($"simulator stalled: seed {seed}, {character}/{bot}, {g.Phase}");
    }
    public static void Run(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        int runs=300,stake=1;int? trace=null;string bot="all",character="all",set="best",unlock="none";bool coins=false;
        for(int i=0;i<args.Length;i++)
        {
            string Next(){if(++i>=args.Length)throw new ArgumentException("missing value for "+args[i-1]);return args[i];}
            switch(args[i])
            {
                case "--runs":runs=int.Parse(Next());break;case "--stake":stake=int.Parse(Next());break;case "--trace":trace=int.Parse(Next());break;
                case "--bot":bot=Next();break;case "--char":character=Next();break;case "--set":set=Next();break;case "--unlock":unlock=Next();break;case "--coins":coins=true;break;
                case "--quota":case "--payout":
                    bool quota=args[i]=="--quota";var numbers=Next().Split(',').Select(double.Parse).ToArray();
                    if(numbers.Length>Game.Route.Count||numbers.Any(n=>!double.IsFinite(n)||n<0))throw new ArgumentException("invalid route overrides");
                    for(int n=0;n<numbers.Length;n++)if(quota)Game.Route[n].PerCoin=numbers[n];else Game.Route[n].Payout=numbers[n];break;
                default:throw new ArgumentException("unknown argument: "+args[i]);
            }
        }
        var bots=new[]{"random","greedy","smart"};
        if(runs<1||stake<1||stake>Game.Stakes.Count||!bots.Append("all").Contains(bot)||!Content.CharacterOrder.Append("all").Contains(character)||!new[]{"best","default"}.Contains(set)||!new[]{"none","all"}.Contains(unlock))throw new ArgumentException("invalid simulator options");
        if(coins)
        {
            var rows=Content.CoinOrder.Select(c=>(id:c.Id,m:Measure(c.Id,100,100))).ToList();
            var medians=rows.GroupBy(r=>Content.Coins[r.id].Rarity).ToDictionary(group=>group.Key,group=>group.Select(r=>r.m.Value).OrderBy(v=>v).ElementAt((group.Count()-1)/2));
            Console.WriteLine("coin         rar  cost     pts   gold    pen    nrg   value  vs rarity median");
            foreach(var r in rows.OrderByDescending(r=>r.m.Value))
            {
                var c=Content.Coins[r.id];double med=medians[c.Rarity],ratio=med==0?0:r.m.Value/med;
                Console.WriteLine($"{r.id,-12} {c.Rarity,-3} {c.Cost,5}  {r.m.Points,6:F2} {r.m.Gold,6:F2} {r.m.Penalty,6:F2} {r.m.Energy,6:F2} {r.m.Value,7:F2} {ratio,6:F2}x{(ratio>1.6?"  <-- strong":ratio<.4?"  <-- weak":"")}");
            }
            Console.WriteLine("value = pts - penalty + 0.5*gold + 0.3*energy - 0.5*energy cost per flip; ignores extra draws, odds boosts and discard synergies.");return;
        }
        if(trace.HasValue)
        {
            var g=Play(trace.Value,character=="all"?"blade":character,bot=="all"?"smart":bot,unlock=="all",set=="default",stake);
            foreach(string line in g.Log)Console.WriteLine(line);
            Console.WriteLine($"END: {g.Phase}, cleared {g.Cleared}, deck {string.Join(", ",g.Coins.Select(c=>c.Id))}");return;
        }
        Console.WriteLine("route (quota per coin/payout): "+string.Join("  ",Game.Route.Select(s=>$"{s.PerCoin}/{s.Payout}"))+"   runs per row: "+runs+(unlock=="all"?"   all locked coins unlocked":""));
        foreach(string c in Content.CharacterOrder.Where(c=>character=="all"||c==character))foreach(string b in bots.Where(b=>bot=="all"||b==bot))
        {
            var reached=new int[Game.Route.Count+1];int wins=0;double gold=0,tokens=0;
            for(int seed=1;seed<=runs;seed++){var g=Play(seed,c,b,unlock=="all",set=="default",stake);for(int level=0;level<=g.Cleared&&level<reached.Length;level++)reached[level]++;if(g.Phase==Phase.Victory)wins++;gold+=g.Player.Gold;tokens+=Game.RunTokens(g);}
            string Pct(int n)=>(100.0*n/runs).ToString("F1").PadLeft(5)+"%";
            Console.WriteLine($"{c,-8} {b,-7} clear L1 {Pct(reached[1])}  L3 {Pct(reached[3])}  L5 {Pct(reached[5])}  L7 {Pct(reached[7])}  boss {Pct(wins)}   avg gold {gold/runs,5:F1}  avg tokens {tokens/runs,4:F2}");
        }
    }
}
