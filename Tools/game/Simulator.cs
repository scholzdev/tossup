using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Tossup;

// Balance tools use the same rules and event bus as the player. Measurements are
// isolated neutral pouches; full runs retain enemies, the map and augments.
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
                // a fight of ten copies of the coin and no enemy; the fight never ends (the round counter is held at 1)
                var g=Game.New(seed,"sim",applyRunEncounter:false);g.Player.Gold=30;
                var e=g.Encounter;g.Coins.Clear();e.Hand.Clear();e.Pouch.Clear();e.Discard.Clear();e.EnemyId=null;
                for(int i=0;i<10;i++){var coin=Game.NewCoin(g,id);g.Coins.Add(coin);e.Pouch.Add(coin.Uid);}
                for(int step=1;step<=flips;step++)
                {
                    if(step%15==1){e.Doubler=0;e.Tails=0;}
                    g.Player.Energy=Math.Max(g.Player.Energy,3);
                    if(e.Hand.Count==0){e.Round=1;Game.EndRound(g);if(e.Hand.Count==0)break;}
                    if(Game.Flip(g,e.Hand[0]))Game.Resolve(g);
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
    // Flip a hand coin and resolve it. smart: re-flip a Tails when the energy left is not needed for the rest of the hand.
    static void FlipCoin(GameState g,int uid,bool smart=false)
    {
        if(!Game.Flip(g,uid))return;
        if(smart&&g.Pending.Result==Side.Tails&&Game.CanReroll(g)&&g.Player.Energy>g.Encounter.Hand.Sum(u=>Game.FlipCost(g,u)))Game.Reroll(g);
        Game.Resolve(g);
    }
    // One round: flip every coin of the hand you can pay for, then end the round. smart: setup coins (buffs, odds) go
    // first, then the strongest coins, and chips are used when behind.
    static void PlayRound(GameState g,string bot,Dictionary<string,double> values)
    {
        var e=g.Encounter;
        if(bot!="smart")
        {
            foreach(int uid in e.Hand.ToList())FlipCoin(g,uid);
            Game.EndRound(g);return;
        }
        bool Use(string id){int slot=g.Items.IndexOf(id);return slot>=0&&Game.UseItem(g,slot);}
        double Points(int uid)=>Content.Coins[Game.GetCoin(g,uid).Id].Heads.Where(x=>x.Type==EffectType.Score).Sum(x=>x.Amount);
        bool Setup(int uid){var d=Content.Coins[Game.GetCoin(g,uid).Id];return d.Buffs.Count>0||d.Heads.Concat(d.Tails).Any(x=>x.Type==EffectType.NextMult||x.Type==EffectType.NextOdds||x.Type==EffectType.ComboShield||x.Type==EffectType.Amplify||x.Type==EffectType.NextHeads);}
        while(g.Phase==Phase.Encounter&&!e.Cleared)
        {
            var playable=e.Hand.Where(u=>Game.CanFlip(g,u)).OrderBy(u=>Setup(u)?0:1).ThenByDescending(Points).ToList();
            if(playable.Count==0)break;
            int next=playable[0];
            bool behind=e.Round>=3&&e.Scored<e.EnemyScore;
            if(behind){if(Points(next)>=3)Use("double_down");if(Points(next)>=2)Use("force_heads");Use("weighted");}
            if(e.Hand.Count<=2&&behind)Use("extra_draw");
            FlipCoin(g,next,true);
        }
        if(g.Phase==Phase.Encounter&&!e.Cleared)Game.EndRound(g);
    }
    static void Shop(GameState g,string bot,Dictionary<string,double> values)
    {
        if(bot=="random"){for(int n=0;n<8;n++)if(!Game.Buy(g,(int)(g.RngState%4)))break;return;}
        while(g.Coins.Count<Game.DeckMax)
        {
            int best=-1;double bestValue=double.NegativeInfinity;
            for(int i=0;i<g.ShopOffers.Count;i++)
            {
                string id=g.ShopOffers[i]?.Id;if(id==null||g.Player.Gold<Game.CoinOfferCost(g,i))continue;
                double v=bot=="smart"?values[id]:(Content.Coins[id].Cost);
                if(v>bestValue){best=i;bestValue=v;}
            }
            if(best<0||(bot=="smart"&&bestValue<=values["normal"]))break;
            if(!Game.Buy(g,best))break;
        }
        if(bot!="smart")return;
        // thin the pouch: melt the weakest coin while it is clearly below average
        for(int n=0;n<2&&g.Player.Gold>=14;n++)
        {
            double mean=g.Coins.Average(c=>values[c.Id]);var worst=g.Coins.OrderBy(c=>values[c.Id]).First();
            if(values[worst.Id]>=mean*.5||!Game.Remove(g,worst.Uid))break;
        }
        if(g.ShopRelic!=null&&g.Player.Gold>=25)Game.BuyRelic(g);
        for(int i=0;i<g.ShopItems.Count;i++)if(new[]{"force_heads","double_down","weighted","extra_draw"}.Contains(g.ShopItems[i]))Game.BuyItem(g,i);
    }
    // Pick the next map node (smart: shop with gold, mint when poor, elites only with a full deck) or answer a Mint / Back Room.
    static void MapStep(GameState g,string bot)
    {
        if(g.MapPrompt!=null){var normal=g.MapPrompt=="mint"?g.Coins.Find(c=>c.Id=="normal"):null;if(normal!=null&&Game.MapChoose(g,"stamp",normal.Uid))return;if(!Game.MapChoose(g,g.MapPrompt=="mint"?"energy":"gold")&&!Game.MapChoose(g,"gold"))throw new InvalidOperationException("map prompt stuck");return;}
        var options=Enumerable.Range(0,g.Map.Count).Where(i=>Game.MapReachable(g,i)).ToList();
        int Priority(NodeKind k)=>k switch{
            NodeKind.Boss=>9,NodeKind.Shop=>g.Player.Gold>=15?5:0,NodeKind.Mint=>g.Player.Gold<15?4:1,
            NodeKind.Elite=>g.Cleared>=2?4:-1,NodeKind.Altar=>3,NodeKind.Event=>3,_=>2};
        int pick=bot=="smart"?options.OrderByDescending(i=>Priority(g.Map[i].Kind)).First():options[(int)(g.RngState%options.Count)];
        if(!Game.ChooseNode(g,pick))throw new InvalidOperationException("map node refused");
    }
    // Per-fight results of every simulated run, for tuning: stage index -> (fights, wins, your points, enemy points).
    static readonly Dictionary<string,(int fights,int wins,double you,double enemy,double youSq)> fights=new();
    static bool bossSeen;
    static void Record(GameState g,bool won)
    {
        var e=g.Encounter;var d=Game.EnemyOf(g);string key=(e.Boss?"boss":(e.Name.StartsWith("Elite ")?"elite ":"fight ")+g.EncounterIndex)+(d!=null?" "+d.Id:"");
        fights.TryGetValue(key,out var t);fights[key]=(t.fights+1,t.wins+(won?1:0),t.you+e.Scored,t.enemy+e.EnemyScore,t.youSq+e.Scored*e.Scored);
    }
    static GameState Play(int seed,string character,string bot,bool all,bool defaults,int stake,List<string> fixedLoadout=null)
    {
        // Measure before constructing the run: Game.New rebinds global rule hooks.
        var values=bot=="smart"||!defaults ? Content.CoinOrder.ToDictionary(c=>c.Id,c=>Value(c.Id)) : null;
        var loadout=fixedLoadout??(defaults?null:BestSet(character,all));
        var g=Game.New(seed,character,Unlocks(character,all),loadout?.Select(id=>Content.Coins[id]).ToList(),stake,map:true);
        bossSeen=false;Encounter recorded=null;
        for(int guard=0;guard<4000;guard++)
        {
            if(g.Phase==Phase.Victory||g.Phase==Phase.GameOver)
            {
                if(g.Encounter!=null&&recorded!=g.Encounter&&!(g.Phase==Phase.Victory&&false)){recorded=g.Encounter;Record(g,g.Phase==Phase.Victory);}
                return g;
            }
            if(g.Phase==Phase.Augment)
            {
                if(g.AugmentPending!=null){var choices=Game.AugmentChoices(g);if(choices.Count==0)throw new InvalidOperationException("augment has no choices");Game.ChooseAugmentOption(g,choices[0].Key);}
                else Game.ChooseAugment(g,g.AugmentOptions[0]);
                continue;
            }
            if(g.Phase==Phase.Map){MapStep(g,bot);continue;}
            if(g.Phase==Phase.Shop){Shop(g,bot,values);Game.LeaveShop(g);continue;}
            if(g.Encounter.Boss)bossSeen=true;
            if(g.Encounter.Cleared){if(recorded!=g.Encounter){recorded=g.Encounter;Record(g,true);}Game.EndLevel(g);}
            else PlayRound(g,bot,values);
        }
        throw new InvalidOperationException($"simulator stalled: seed {seed}, {character}/{bot}, {g.Phase}");
    }
    static double DeckScore(string character,List<string> deck,int runs,int stake)
    {
        double score=0;
        for(int seed=1;seed<=runs;seed++)
        {
            var g=Play(seed,character,"smart",true,false,stake,deck);
            score+=g.Cleared+(g.Phase==Phase.Victory?8:0);
        }
        return score/runs;
    }
    static bool CanAdd(List<string> deck,string id)
        => deck.Count<Game.StartMax&&deck.Count(x=>Content.Coins[x].Rarity==Content.Coins[id].Rarity)<Profile.RarityLimit(id);
    static List<string> RandomDeck(List<string> candidates,Random rng)
    {
        var deck=new List<string>();
        while(deck.Count<Game.StartMax)
        {
            var available=candidates.Where(id=>CanAdd(deck,id)).ToList();
            if(available.Count==0)break;
            deck.Add(available[rng.Next(available.Count)]);
        }
        return deck;
    }
    static List<string> OptimizeDeck(string character,int searchRuns,int stake)
    {
        var candidates=Content.Characters[character].Pool.Select(c=>c.Id).Concat(Unlocks(character,true)).Distinct().OrderBy(id=>id,StringComparer.Ordinal).ToList();
        var starts=new List<List<string>>{BestSet(character,true)};
        var rng=new Random(20261006+Content.CharacterOrder.IndexOf(character));
        for(int i=0;i<6;i++)starts.Add(RandomDeck(candidates,rng));
        List<string> best=null;double bestScore=double.NegativeInfinity;
        foreach(var start in starts)
        {
            var current=start.ToList();double currentScore=DeckScore(character,current,searchRuns,stake);
            for(int pass=0;pass<2;pass++)
            {
                var next=current;double nextScore=currentScore;
                for(int slot=0;slot<current.Count;slot++)
                foreach(string id in candidates)
                {
                    var trial=current.ToList();trial[slot]=id;
                    if(trial.Count(x=>Content.Coins[x].Rarity==Content.Coins[id].Rarity)>Profile.RarityLimit(id))continue;
                    double score=DeckScore(character,trial,searchRuns,stake);
                    if(score>nextScore){next=trial;nextScore=score;}
                }
                if(nextScore<=currentScore)break;
                current=next;currentScore=nextScore;
            }
            if(currentScore>bestScore){best=current;bestScore=currentScore;}
        }
        return best;
    }
    static void RunDeckOptimization(int runs,int searchRuns,int stake)
    {
        Console.WriteLine($"Optimizing legal {Game.StartMax}-coin starting decks over {searchRuns} search seeds; validating on {runs} separate seeds.");
        foreach(string character in Content.CharacterOrder)
        {
            var deck=OptimizeDeck(character,searchRuns,stake);
            var reached=new int[Game.Route.Count+1];int wins=0;double cleared=0;
            var baselineReached=new int[Game.Route.Count+1];int baselineWins=0;double baselineCleared=0;
            for(int seed=10001;seed<10001+runs;seed++)
            {
                var g=Play(seed,character,"smart",true,false,stake,deck);
                for(int level=0;level<=g.Cleared&&level<reached.Length;level++)reached[level]++;
                if(g.Phase==Phase.Victory)wins++;cleared+=g.Cleared;
                var baseline=Play(seed,character,"smart",true,true,stake);
                for(int level=0;level<=baseline.Cleared&&level<baselineReached.Length;level++)baselineReached[level]++;
                if(baseline.Phase==Phase.Victory)baselineWins++;baselineCleared+=baseline.Cleared;
            }
            string Pct(int n)=>(100.0*n/runs).ToString("F1")+"%";
            Console.WriteLine($"{character,-10} deck [{string.Join(",",deck)}]");
            Console.WriteLine($"  optimized: L1 {Pct(reached[1]),6} L3 {Pct(reached[3]),6} L5 {Pct(reached[5]),6} L7 {Pct(reached[7]),6} boss {Pct(wins),6} avg cleared {cleared/runs:F2}");
            Console.WriteLine($"  starter:   L1 {Pct(baselineReached[1]),6} L3 {Pct(baselineReached[3]),6} L5 {Pct(baselineReached[5]),6} L7 {Pct(baselineReached[7]),6} boss {Pct(baselineWins),6} avg cleared {baselineCleared/runs:F2}");
        }
    }
    public static void Run(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        int runs=300,searchRuns=40,stake=1;int? trace=null;string bot="all",character="all",set="best",unlock="none";bool coins=false,optimize=false;
        for(int i=0;i<args.Length;i++)
        {
            string Next(){if(++i>=args.Length)throw new ArgumentException("missing value for "+args[i-1]);return args[i];}
            switch(args[i])
            {
                case "--runs":runs=int.Parse(Next());break;case "--stake":stake=int.Parse(Next());break;case "--trace":trace=int.Parse(Next());break;
                case "--bot":bot=Next();break;case "--char":character=Next();break;case "--set":set=Next();break;case "--unlock":unlock=Next();break;case "--coins":coins=true;break;
                case "--optimize":optimize=true;break;case "--search-runs":searchRuns=int.Parse(Next());break;
                case "--payout":
                    var numbers=Next().Split(',').Select(double.Parse).ToArray();
                    if(numbers.Length>Game.Route.Count||numbers.Any(n=>!double.IsFinite(n)||n<0))throw new ArgumentException("invalid route overrides");
                    for(int n=0;n<numbers.Length;n++)Game.Route[n].Payout=numbers[n];break;
                default:throw new ArgumentException("unknown argument: "+args[i]);
            }
        }
        var bots=new[]{"random","greedy","smart"};
        if(runs<1||searchRuns<1||stake<1||stake>Game.Stakes.Count||!bots.Append("all").Contains(bot)||!Content.CharacterOrder.Append("all").Contains(character)||!new[]{"best","default"}.Contains(set)||!new[]{"none","all"}.Contains(unlock))throw new ArgumentException("invalid simulator options");
        if(optimize){RunDeckOptimization(runs,searchRuns,stake);return;}
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
        Console.WriteLine("route payouts: "+string.Join("  ",Game.Route.Select(s=>s.Payout?.ToString()??"-"))+"   runs per row: "+runs+(unlock=="all"?"   all locked coins unlocked":""));
        foreach(string c in Content.CharacterOrder.Where(c=>character=="all"||c==character))foreach(string b in bots.Where(b=>bot=="all"||b==bot))
        {
            var reached=new int[Game.Route.Count+1];int wins=0,bosses=0;double gold=0,tokens=0;
            for(int seed=1;seed<=runs;seed++){var g=Play(seed,c,b,unlock=="all",set=="default",stake);for(int level=0;level<=g.Cleared&&level<reached.Length;level++)reached[level]++;if(g.Phase==Phase.Victory)wins++;if(bossSeen)bosses++;gold+=g.Player.Gold;tokens+=Game.RunTokens(g);}
            string Pct(int n)=>(100.0*n/runs).ToString("F1").PadLeft(5)+"%";
            Console.WriteLine($"{c,-9} {b,-7} first fight {Pct(reached[1])}  3 fights {Pct(reached[3])}  5 fights {Pct(reached[5])}  reach boss {Pct(bosses)}  beat boss {Pct(wins)}   avg gold {gold/runs,5:F1}  avg tokens {tokens/runs,4:F2}");
        }
        if(fights.Count>0)
        {
            Console.WriteLine("fight                   n   win%   you  (sd)  enemy");
            foreach(var k in fights.Keys.OrderBy(k=>k,StringComparer.Ordinal)){var t=fights[k];Console.WriteLine($"{k,-22}{t.fights,5} {100.0*t.wins/t.fights,5:F0}% {t.you/t.fights,6:F1} ({Math.Sqrt(Math.Max(0,t.youSq/t.fights-Math.Pow(t.you/t.fights,2))),4:F0}) {t.enemy/t.fights,6:F1}");}
        }
    }
}
