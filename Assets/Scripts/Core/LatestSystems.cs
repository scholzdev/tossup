using System;
using System.Collections.Generic;

namespace Tossup
{
    public sealed class SideBetQuote { public string Side; public int Stake, Payout; }

    public static partial class Game
    {
        public static readonly List<string> ModifierOrder = new List<string>
            { "lucky_day", "cold_snap", "power_surge", "blackout", "gold_rush", "high_stakes", "good_rhythm", "bonus_exchange" };
        public static readonly Dictionary<string, ModifierDef> Modifiers = BuildModifiers();
        public static readonly List<StakeDef> Stakes = BuildStakes();
        public static readonly Dictionary<string, ContractDef> Contracts = BuildContracts();
        public static readonly List<string> ContractOrder = new List<string> { "quick_clear", "clean_run", "hot_streak", "bank_once", "amazon_prime" };
        public static readonly Dictionary<string, RunEncounterDef> Encounters = BuildEncounters();
        public static readonly List<string> EncounterOrder = new List<string> { "house_clock", "dead_heat", "high_roller_table", "thin_market", "upgrade" };
        public static readonly Dictionary<string, AugmentDef> AugmentDefs = BuildAugments();
        public static readonly List<string> AugmentOrder = new List<string> { "bankers_cut", "all_in", "hedge_fund", "scrap_dealer", "epic_windfall", "reforger", "type_specialist", "upgrade_press" };

        static Dictionary<string, ModifierDef> BuildModifiers()
        {
            var d = new Dictionary<string, ModifierDef>();
            void Add(string id, string name, string text, Action<GameState,Encounter> apply) => d[id]=new ModifierDef{Id=id,Name=name,Description=text,Apply=apply};
            Add("lucky_day","Lucky Day","All coins +10% Heads.",(g,e)=>e.Magnet+=.10);
            Add("cold_snap","Cold Snap","All coins -10% Heads, payout +40%.",(g,e)=>{e.Magnet-=.10;e.Payout=Math.Floor((e.Payout??0)*1.4+.5);});
            Add("power_surge","Power Surge","Start with 5 energy.",(g,e)=>g.Player.Energy=5);
            Add("blackout","Blackout","Start with 1 energy, payout +40%.",(g,e)=>{g.Player.Energy=1;e.Payout=Math.Floor((e.Payout??0)*1.4+.5);});
            Add("gold_rush","Gold Rush","Gold effects pay double.",(g,e)=>e.GoldMult=2);
            Add("high_stakes","High Stakes","Quota +25%, payout +50%.",(g,e)=>{e.Quota=Math.Floor(e.Quota*1.25+.5);e.MaxQuota=e.Quota;e.Payout=Math.Floor((e.Payout??0)*1.5+.5);});
            Add("good_rhythm","Good Rhythm","Combo grows +0.4 per step.",(g,e)=>e.ComboStep=.4);
            Add("bonus_exchange","Bonus Exchange","One extra exchange this level.",(g,e)=>e.ExtraExchanges++);
            return d;
        }

        static List<StakeDef> BuildStakes()
        {
            StakeDef S(string text,string info,string key=null,double value=0){var s=new StakeDef{Text=text,Info=info};if(key!=null)s.Rules[key]=value;return s;}
            return new List<StakeDef>{S("NO CHANGES","No changes"),S("QUOTAS +15%","Quotas +15%","quota_mult",1.15),S("START WITH 15 GOLD","Start with 15 gold","start_gold",15),S("THE HOUSE INVERTS EVERY 4TH FLIP","The House inverts every 4th flip","boss_every",4),S("SHOP PRICES +20%","Shop prices +20%","price_mult",1.2),S("ONLY 2 EXCHANGES PER LEVEL","Only 2 exchanges per level","exchange_max",2),S("LEVEL 1 HAS A MODIFIER TOO","Level 1 has a modifier too","modifiers_from",1),S("QUOTAS +80% IN TOTAL","Quotas +80% in total","quota_mult",1.8)};
        }

        static Dictionary<string, ContractDef> BuildContracts()
        {
            return new Dictionary<string, ContractDef>{
                ["quick_clear"]=new ContractDef{Id="quick_clear",Name="QUICK CLEAR",Description="Clear the quota within 6 flips.",Drawback="Heads odds are reduced by 5 percentage points.",Reward=5,HeadsPenalty=.05,Complete=(g,e)=>e.Flips<=6},
                ["clean_run"]=new ContractDef{Id="clean_run",Name="CLEAN RUN",Description="Clear the quota without discarding a coin.",Drawback="Each Tails increases the quota by 2.",Reward=4,Complete=(g,e)=>e.Discards==0},
                ["hot_streak"]=new ContractDef{Id="hot_streak",Name="HOT STREAK",Description="Reach a combo of 4 before clearing the quota.",Drawback="A Tie breaks your combo and loses its unbanked pot.",Reward=5,Complete=(g,e)=>e.BestComboLen>=4},
                ["bank_once"]=new ContractDef{Id="bank_once",Name="LOCK IT IN",Description="Bank a combo before clearing the quota.",Drawback="Your combo multiplier is capped at x2.",Reward=3,Complete=(g,e)=>e.ComboBanked},
                ["amazon_prime"]=new ContractDef{Id="amazon_prime",Name="AMAZON PRIME",Description="Clear with at least 3 unplayed coins remaining.",Drawback="Miss it and lose up to 2 coins from your stack.",RewardText="HALF PRICE REROLLS",Complete=(g,e)=>e.Queue.Count+e.Pile.Count>=3,RewardAction=(g,e)=>{g.RerollCost=Math.Max(1,g.RerollCost/2);g.RerollStep=Math.Max(1,g.RerollStep/2);}}
            };
        }

        static Dictionary<string, RunEncounterDef> BuildEncounters()
        {
            return new Dictionary<string, RunEncounterDef>{
                ["house_clock"]=new RunEncounterDef{Id="house_clock",Name="The House's Clock",Description="Every third flip is inverted. Clearing a level pays 25% more."},
                ["dead_heat"]=new RunEncounterDef{Id="dead_heat",Name="Dead Heat",Description="A Tie breaks your combo and burns its pot. Banking a combo grants 2 extra gold."},
                ["high_roller_table"]=new RunEncounterDef{Id="high_roller_table",Name="High-Roller Table",Description="Side bets use double the stake and payout. A Tie loses the stake."},
                ["thin_market"]=new RunEncounterDef{Id="thin_market",Name="Thin Market",Description="Shops show one fewer coin, but every coin costs 2 fewer gold."},
                ["upgrade"]=new RunEncounterDef{Id="upgrade",Name="Upgrade",Description="Start with a random Common coin."}
            };
        }

        static Dictionary<string, AugmentDef> BuildAugments()
        {
            var d=new Dictionary<string,AugmentDef>();
            void A(string id,string name,string desc,string tier="silver")=>d[id]=new AugmentDef{Id=id,Name=name,Description=desc,Tier=tier};
            A("bankers_cut","Banker's Cut","Banked combo pots grant 2 extra gold. Each bank adds 2 quota to the next level.");
            A("all_in","All-In","The first successful push each level doubles its combo payout. A failed push costs 2 gold.");
            A("hedge_fund","Hedge Fund","A winning side bet pays 25% extra. Losing one adds 2 quota to the next level.");
            A("scrap_dealer","Scrap Dealer","Discarding a coin grants 1 gold, but each discard adds 2 quota to the current level.");
            A("epic_windfall","Epic Windfall","Gain a random Epic coin. If your bank is full, choose a coin to replace.","gold");
            A("reforger","Reforger","Reforge one coin now into a random different coin of the same rarity. Its upgrade is lost.");
            A("type_specialist","Type Specialist","Choose a coin type you own. Each coin of that type scores +1 on its first Heads each level.");
            A("upgrade_press","Upgrade Press","Choose an owned coin and give it one of its available upgrades.");
            return d;
        }

        public static bool HasType(CoinInst coin, CoinType type)
        { foreach(var candidate in coin.Definition.Types)if(candidate==type)return true;return false; }
        public static bool HasType(CoinInst coin, string type)
        { foreach(var candidate in coin.Definition.Types)if(DefinitionKeys.Key(candidate)==type)return true;return false; }
        static bool IsTypeBuff(string kind) => kind=="steel"||kind=="blood"||kind=="greed"||kind=="chaos"||kind=="rhythm";

        public static IReadOnlyList<Effect> TieEffects(string id) => Content.Coins[id].Edge;

        static int ActiveTypeBuffs(Encounter e,string kind){int n=0;foreach(var b in e.Buffs)if(!b.Fresh&&b.Kind==kind)n++;return n;}
        static void ApplyTypeBuffs(GameState g,CoinInst item,string final,bool comboBroke,List<Effect> effects)
        {
            foreach(var b in g.Encounter.Buffs)
            {
                if(b.Fresh||!IsTypeBuff(b.Kind)||!HasType(item,b.Kind))continue;
                if(b.Kind=="steel"){if(final==Side.Heads)effects.Add(new Effect(EffectType.Score,3));else if(final==Side.Tails)effects.Add(new Effect(EffectType.Penalty,2));}
                else if(b.Kind=="blood"){if(final==Side.Heads||final==Side.Tie)effects.Add(new Effect(EffectType.Score,final==Side.Tie?4:8));if(final==Side.Tails||final==Side.Tie)effects.Add(new Effect(EffectType.Penalty,final==Side.Tie?2:4));}
                else if(b.Kind=="chaos"){int count=effects.Count;for(int i=0;i<count;i++)effects.Add(effects[i].Copy());}
                else if(b.Kind=="rhythm"){if(comboBroke)effects.Add(new Effect(EffectType.Penalty,5));else{double points=Math.Min(8,2*Math.Max(0,g.Encounter.ComboLen-1));if(points>0)effects.Add(new Effect(EffectType.Score,points));}}
            }
        }

        public static bool PushCombo(GameState g)
        {
            if(g==null||g.Phase!=Phase.Encounter||g.Pending!=null||g.Dealt==null||g.Encounter.ComboSide==null)return false;
            g.Encounter.PushUsed=true;
            if(Flip(g))return true;
            g.Encounter.PushUsed=false;
            return false;
        }

        public static double Rule(GameState game, string key, double fallback)
        {
            double value=fallback;
            for(int i=0;i<Math.Min(game.Stake,Stakes.Count);i++) if(Stakes[i].Rules.TryGetValue(key,out var v)) value=v;
            return value;
        }

        public static int Price(GameState game, double value) => (int)Math.Floor(value*Rule(game,"price_mult",1)+.5);

        static double PrintedNet(IReadOnlyList<Effect> effects){double n=0;foreach(var e in effects){if(e.Type==EffectType.Score)n+=e.Amount;else if(e.Type==EffectType.Penalty)n-=e.Amount;}return n;}

        static double DeckQuota(GameState game)
        {
            double power=0;
            foreach(var coin in game.Coins){var d=coin.Definition;SandboxOdds odds=null;game.Sandbox?.Odds.TryGetValue(coin.Id,out odds);double tie=odds?.Tie??d.TieProbability;double heads=Math.Max(0,Math.Min(1-tie,(odds?.Heads??d.Probability)+coin.Bonus+(HasType(coin,"fortune")?game.FortuneBonus:0)));if(coin.Upgrade!=null&&d.TryGetUpgrade(coin.Upgrade.Id,out var u))heads=Math.Min(1-tie,heads+(u.Type==UpgradeType.Probability?u.Value:0));double tails=1-heads-tie;double expected=heads*PrintedNet(d.Heads)+tails*PrintedNet(d.Tails)+tie*PrintedNet(TieEffects(coin.Id));if(coin.Upgrade!=null&&d.TryGetUpgrade(coin.Upgrade.Id,out var up))expected+=heads*(up.Type==UpgradeType.ScoreBonus?up.Value:0);
                expected+=d.EstimateExtraScore(game,coin,heads,tails);power+=Math.Max(0,expected);}
            double mult=Rule(game,"quota_mult",1), basis=QuotaFor(game.EncounterIndex,game.Coins.Count,mult), excess=Math.Max(0,power-1.5*game.Coins.Count);
            return basis+Math.Floor(excess*1.3*mult+.5);
        }

        static void ApplyModifier(GameState game)
        {
            if (game.Sandbox != null) return;
            if(game.EncounterIndex<(int)Rule(game,"modifiers_from",2))return;
            string id=ModifierOrder[Rng.Int(game,1,ModifierOrder.Count)-1];game.Encounter.Modifier=id;Modifiers[id].Apply(game,game.Encounter);
        }

        static bool HasAugment(GameState g,string id)=>g.Augments.Contains(id);

        static void TriggerRunHook(GameState g,string evt)
        {
            var e=g.Encounter;
            if(g.RunEncounterId=="thin_market"&&evt=="run_start"){g.Shop.CoinOfferCount=3;g.Shop.CoinPriceDiscount=2;}
            if(g.RunEncounterId=="upgrade"&&evt=="run_start"){
                var pool=new List<CoinDef>();foreach(var coin in UsablePool(g))if(coin.Rarity==Rarity.Common)pool.Add(coin);
                if(pool.Count>0){if(g.Coins.Count>=g.Slots&&g.Slots<DeckMax)g.Slots++;AddToDeck(g,pool[Rng.Int(g,1,pool.Count)-1]);}
            }
            if(g.RunEncounterId=="house_clock"&&evt=="encounter_start"&&e?.Payout!=null)e.Payout=Math.Floor(e.Payout.Value*1.25+.5);
            if(HasAugment(g,"scrap_dealer")&&evt=="discard"&&e!=null){g.Player.Gold++;e.MaxQuota+=2;if(!e.Cleared)e.Quota+=2;Log(g,"Scrap Dealer: +1 gold, quota +2.");}
        }

        public static bool OfferContract(GameState g)
        {
            if(!g.ContractsEnabled||g.Phase!=Phase.Encounter||g.Encounter==null||g.Encounter.Flips>0)return false;
            var pool=new List<string>(ContractOrder);if(g.Encounter.Boss)pool.Remove("amazon_prime");g.Encounter.ContractOptions=Offers(g,pool,3);g.Phase=Phase.Contract;return true;
        }
        public static bool ChooseContract(GameState g,string id){if(g.Phase!=Phase.Contract||g.Encounter.ContractOptions==null||!g.Encounter.ContractOptions.Contains(id))return false;g.Encounter.Contract=new ContractState{Id=id,StartGold=g.Player.Gold,StartDiscards=g.Encounter.Discards};g.Encounter.ContractOptions=null;g.Phase=Phase.Encounter;Log(g,"Accepted contract: "+Contracts[id].Name+".");return true;}
        public static bool SkipContract(GameState g){if(g.Phase!=Phase.Contract)return false;g.Encounter.ContractOptions=null;g.Phase=Phase.Encounter;Log(g,"Contract skipped.");return true;}
        static void SettleContract(GameState g)
        {
            var c = g.Encounter.Contract;
            if (c == null || c.Result != null) return;
            var d = Contracts[c.Id];
            bool ok = d.Complete == null || d.Complete(g, g.Encounter);
            c.Result = ok ? "COMPLETE" : "MISSED";
            if (ok) { g.Player.Gold += d.Reward; Log(g, "Contract complete: +" + d.Reward + " gold."); }
            else Log(g, "Contract missed: " + d.Name + ".");
        }

        public static bool OfferAugment(GameState g,int level)
        {if(g.Phase!=Phase.Shop||g.Sandbox!=null||(level!=3&&level!=6))return false;var pool=new List<string>();foreach(var id in AugmentOrder)if(!g.Augments.Contains(id)&&AugmentAvailable(g,id))pool.Add(id);if(pool.Count<3)return false;g.AugmentLevel=level;g.AugmentOptions=Offers(g,pool,3);g.Phase=Phase.Augment;return true;}
        static bool AugmentAvailable(GameState g,string id)
        {
            if (id == "epic_windfall")
            {
                if (g.EncounterIndex + 1 != 6) return false;
                foreach (var coin in Content.CoinOrder) if (coin.Rarity == Rarity.Epic) return true;
                return false;
            }
            if (id == "reforger")
            {
                foreach (var owned in g.Coins)
                    foreach (var coinDef in Content.CoinOrder)
                        if (coinDef.Id != owned.Id && coinDef.Rarity == owned.Definition.Rarity) return true;
                return false;
            }
            if (id == "type_specialist")
            {
                foreach (var coin in g.Coins) if (coin.Definition.Types.Count > 0) return true;
                return false;
            }
            if (id == "upgrade_press")
            {
                foreach (var coin in g.Coins) if (coin.Upgrade == null && coin.Definition.Upgrades.Count > 0) return true;
                return false;
            }
            return true;
        }
        public static bool ChooseAugment(GameState g,string id){if(g.Phase!=Phase.Augment||g.AugmentPending!=null||g.AugmentOptions==null||!g.AugmentOptions.Contains(id))return false;g.Augments.Add(id);g.AugmentOptions=null;Log(g,"Chosen augment: "+AugmentDefs[id].Name+".");if(id=="epic_windfall"){var ids=new List<string>();foreach(var x in Content.CoinOrder)if(x.Rarity==Rarity.Epic)ids.Add(x.Id);string reward=ids[Rng.Int(g,1,ids.Count)-1];if(g.Coins.Count<g.Slots){AddToDeck(g,reward);FinishAugment(g);}else g.AugmentPending=new AugmentPending{Id=id,RewardId=reward};}else if(id=="reforger"||id=="type_specialist"||id=="upgrade_press")g.AugmentPending=new AugmentPending{Id=id};else FinishAugment(g);return true;}
        static void FinishAugment(GameState g){int level=g.AugmentLevel??g.EncounterIndex+1;g.AugmentLevel=null;g.AugmentOptions=null;g.AugmentPending=null;g.EncounterIndex=level;StartEncounter(g);}
        public static List<AugmentChoice> AugmentChoices(GameState g)
        {
            var result = new List<AugmentChoice>();
            var pending = g.AugmentPending;
            if (g.Phase != Phase.Augment || pending == null) return result;
            if (pending.Id == "type_specialist")
            {
                var seen = new HashSet<CoinType>();
                foreach (var coin in g.Coins)
                    foreach (var type in coin.Definition.Types)
                        if (seen.Add(type)) result.Add(new AugmentChoice { Key = DefinitionKeys.Key(type), Title = type.ToString().ToUpperInvariant(), Detail = "First Heads per coin each level: +1 point" });
                result.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
            }
            else if (pending.Id == "upgrade_press")
            {
                foreach (var coin in g.Coins)
                {
                    if (coin.Upgrade != null) continue;
                    var upgrades = coin.Definition.Upgrades;
                    var keys = new List<string>();foreach(var upgrade in upgrades)keys.Add(upgrade.Id);
                    keys.Sort(StringComparer.Ordinal);
                    foreach (var key in keys)
                    {
                        coin.Definition.TryGetUpgrade(key,out var upgrade);
                        result.Add(new AugmentChoice { Key = coin.Uid + ":" + key, CoinId = coin.Id, UpgradeId = key,
                            UpgradeName = upgrade.Name, Detail = upgrade.Description });
                    }
                }
            }
            else
            {
                foreach (var coin in g.Coins)
                {
                    if (pending.Id == "reforger")
                    {
                        bool hasAlternative = false;
                        foreach (var coinDef in Content.CoinOrder)
                            if (coinDef.Id != coin.Id && coinDef.Rarity == coin.Definition.Rarity)
                            { hasAlternative = true; break; }
                        if (!hasAlternative) continue;
                    }
                    result.Add(new AugmentChoice { Key = coin.Uid.ToString(), CoinId = coin.Id, CurrentUpgrade = coin.Upgrade,
                        Detail = pending.Id == "reforger" ? "Random coin of the same rarity." : "Replace this coin." });
                }
            }
            return result;
        }
        public static bool ChooseAugmentOption(GameState g,string key){var p=g.AugmentPending;if(p==null)return false;var options=AugmentChoices(g);AugmentChoice chosen=null;foreach(var x in options)if(x.Key==key)chosen=x;if(chosen==null)return false;if(p.Id=="type_specialist")g.AugmentData["type_specialist"]=key;else if(p.Id=="upgrade_press"){var bits=key.Split(':');var c=GetCoin(g,int.Parse(bits[0]));c.Definition.TryGetUpgrade(bits[1],out var upgrade);c.Upgrade=upgrade;}else{var old=GetCoin(g,int.Parse(key));string id=p.RewardId;if(p.Id=="reforger"){var ids=new List<string>();foreach(var x in Content.CoinOrder)if(x.Id!=old.Id&&x.Rarity==old.Definition.Rarity)ids.Add(x.Id);id=ids[Rng.Int(g,1,ids.Count)-1];}int index=g.Coins.IndexOf(old);var replacement=NewCoin(g,id);g.Coins[index]=replacement;g.SelectedUid=replacement.Uid;}FinishAugment(g);return true;}

        public static SideBetQuote SideBetQuote(GameState g,string side){if(g.Dealt==null||(side!=Side.Heads&&side!=Side.Tails))return null;double odds=side==Side.Heads?g.Dealt.Probability:1-g.Dealt.Probability-g.Dealt.TieProbability;if(odds<=0)return null;int stake=5,payout=Math.Max(stake,(int)Math.Floor(stake/odds+.5));if(g.RunEncounterId=="high_roller_table"){stake*=2;payout*=2;}if(HasAugment(g,"hedge_fund"))payout=(int)Math.Floor(payout*1.25+.5);return new SideBetQuote{Side=side,Stake=stake,Payout=payout};}
        public static bool PlaceSideBet(GameState g,string side){var e=g.Encounter;if(g.Phase!=Phase.Encounter||e==null||e.Flips!=0||g.Dealt==null||e.SideBetSide!=null)return false;var q=SideBetQuote(g,side);if(q==null||g.Player.Gold<q.Stake)return false;g.Player.Gold-=q.Stake;e.SideBetSide=side;e.SideBetOutcome=null;e.SideBetCost=q.Stake;e.SideBetPayout=q.Payout;Log(g,"Bet "+q.Stake+" gold on "+side+" ("+q.Payout+" gold payout).");return true;}

        static double ComboPot(double len)=>Math.Max(0,(len-1)*len/2);
        public static bool CanBankCombo(GameState g)=>g!=null&&g.Phase==Phase.Encounter&&g.Encounter!=null&&g.Pending==null&&g.Mulligan==null&&g.Encounter.ComboPot>0;
        static int BankComboPot(GameState g,bool counts=true){var e=g.Encounter;int amount=(int)Math.Floor(e.ComboPot);if(amount<=0)return 0;if(g.RunEncounterId=="dead_heat")amount+=2;if(HasAugment(g,"bankers_cut")){amount+=2;g.NextLevelQuotaBonus+=2;}g.Player.Gold+=amount;if(counts)e.ComboBanked=true;e.ComboPot=0;e.ComboSide=null;e.ComboLen=0;Log(g,"Banked "+amount+" combo gold.");return amount;}
        public static int BankCombo(GameState g)=>CanBankCombo(g)?BankComboPot(g):0;
        public static bool DiscardBank(GameState g,int uid){var e=g.Encounter;if(e==null||e.BankDiscards<1)return false;int i=e.Queue.IndexOf(uid);if(i<0||i>=Visible||Discard(g,new[]{uid})==0)return false;e.BankDiscards--;return true;}
        public static int ExchangesLeft(GameState g)=>(int)Rule(g,"exchange_max",ExchangeMax)+g.Encounter.ExtraExchanges-g.Encounter.Exchanges;
        public static int SlotPrice(GameState g)=>SlotCost+SlotStep*(g.Slots-StartMax);
        public static bool BuySlot(GameState g){int cost=SlotPrice(g);if(g.Phase!=Phase.Shop||g.Slots>=DeckMax||g.Player.Gold<cost)return false;g.Player.Gold-=cost;g.Slots++;Log(g,"Bought deck slot "+g.Slots+" for "+cost+" gold.");return true;}
        public static int CoinOfferCost(GameState g,int index){if(index<0||index>=g.ShopOffers.Count||g.ShopOffers[index]==null)return -1;var coin=g.ShopOffers[index];int cost=coin.Cost;if(index<g.ShopUpgrades.Count&&g.ShopUpgrades[index]!=null&&coin.TryGetUpgrade(g.ShopUpgrades[index].Id,out var u))cost+=u.Cost;return Math.Max(0,Price(g,cost)-(int)g.Shop.CoinPriceDiscount);}

        static void SetShopStock(GameState g)
        {
            g.ShopOffers=Offers(g,ShopPool(g),g.Shop.CoinOfferCount);g.ShopUpgrades=new List<Upgrade>();
            foreach(var id in g.ShopOffers)g.ShopUpgrades.Add(null);
            var variants = new List<(CoinDef Coin,Upgrade Upgrade)>();
            foreach (var coin in ShopPool(g))
            {
                var upgrades = new List<Upgrade>(coin.Upgrades);
                upgrades.Sort((a,b) => StringComparer.Ordinal.Compare(a.Id,b.Id));
                foreach (var upgrade in upgrades) variants.Add((coin, upgrade));
            }
            if(variants.Count>0&&g.ShopOffers.Count>0&&Rng.Int(g,1,3)==1){var v=variants[Rng.Int(g,1,variants.Count)-1];int index=g.ShopOffers.IndexOf(v.Coin);if(index<0)index=Rng.Int(g,1,g.ShopOffers.Count)-1;g.ShopOffers[index]=v.Coin;g.ShopUpgrades[index]=v.Upgrade;}
        }
    }
}
