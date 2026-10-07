using System;
using System.Collections.Generic;

namespace Tossup
{
    public enum RunHookEvent
    {
        RunStart,
        EncounterStart,
    }

    public static partial class Game
    {
        public static readonly List<string> ModifierOrder = new List<string>
            { "lucky_day", "cold_snap", "power_surge", "blackout", "gold_rush", "high_stakes", "good_rhythm", "wide_hand" };
        public static readonly Dictionary<string, ModifierDef> Modifiers = BuildModifiers();
        public static readonly List<StakeDef> Stakes = BuildStakes();
        public static readonly List<RunEncounterDef> Encounters = EncounterCatalog.Ordered;
        public static readonly Dictionary<string, RunEncounterDef> EncounterById = BuildEncounterIndex();
        public static readonly Dictionary<string, AugmentDef> AugmentDefs = AugmentCatalog.ById;
        public static readonly List<string> AugmentOrder = AugmentCatalog.Ids;

        public static double CharacterPerkValue(GameState game, CharacterPerkType type)
        {
            if (game == null || string.IsNullOrEmpty(game.CharacterId) || !Content.Characters.TryGetValue(game.CharacterId, out var character) || character.Perks == null)
                return 0;

            double value = 0;
            foreach (var perk in character.Perks)
                if (perk.Type == type) value += perk.Value;
            return value;
        }

        static void ApplyCharacterEncounterPerks(GameState game, Encounter encounter)
        {
            encounter.EnergyBonus += (int)CharacterPerkValue(game, CharacterPerkType.StartEnergy);
            encounter.ComboStep += CharacterPerkValue(game, CharacterPerkType.ComboStep);
            encounter.Shield += CharacterPerkValue(game, CharacterPerkType.ComboShield);
        }

        static Dictionary<string, ModifierDef> BuildModifiers()
        {
            var d = new Dictionary<string, ModifierDef>();
            void Add(string id, string name, string text, Action<GameState,Encounter> apply) => d[id]=new ModifierDef{Id=id,Name=name,Description=text,Apply=apply};
            Add("lucky_day","Lucky Day","All coins +10% Heads.",(g,e)=>e.Magnet+=.10);
            Add("cold_snap","Cold Snap","All coins -10% Heads, payout +40%.",(g,e)=>{e.Magnet-=.10;e.Payout=Math.Floor((e.Payout??0)*1.4+.5);});
            Add("power_surge","Power Surge","+2 energy every round.",(g,e)=>e.EnergyBonus+=2);
            Add("blackout","Blackout","-2 energy every round, payout +40%.",(g,e)=>{e.EnergyBonus-=2;e.Payout=Math.Floor((e.Payout??0)*1.4+.5);});
            Add("gold_rush","Gold Rush","Gold effects pay double.",(g,e)=>e.GoldMult=2);
            Add("high_stakes","High Stakes","The enemy flips 1 more coin a round, payout +50%.",(g,e)=>{if(e.EnemyId!=null)e.EnemyDraw++;e.Payout=Math.Floor((e.Payout??0)*1.5+.5);});
            Add("good_rhythm","Good Rhythm","Combo grows +0.4 per step.",(g,e)=>e.ComboStep=.4);
            Add("wide_hand","Wide Hand","Your hand holds 1 more coin.",(g,e)=>e.HandSize++);
            return d;
        }

        static List<StakeDef> BuildStakes()
        {
            StakeDef S(string text,string info,string key=null,double value=0){var s=new StakeDef{Text=text,Info=info};if(key!=null)s.Rules[key]=value;return s;}
            return new List<StakeDef>{S("NO CHANGES","No changes"),S("ENEMIES +1 POINT PER ROUND","Enemies +1 point per round","enemy_round",1),S("START WITH 3 GOLD","Start with 3 gold","start_gold",3),S("THE HOUSE INVERTS EVERY 4TH FLIP","The House inverts every 4th flip","boss_every",4),S("SHOP PRICES +20%","Shop prices +20%","price_mult",1.2),S("HAND HOLDS 4 COINS","Your hand holds 4 coins","hand_size",4),S("LEVEL 1 HAS A MODIFIER TOO","Level 1 has a modifier too","modifiers_from",1),S("ENEMIES +3 POINTS PER ROUND IN TOTAL","Enemies +3 points per round in total","enemy_round",3)};
        }

        static Dictionary<string, RunEncounterDef> BuildEncounterIndex()
        {
            var result=new Dictionary<string,RunEncounterDef>();
            foreach(var encounter in Encounters)result.Add(encounter.Id,encounter);
            return result;
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
                if(b.Kind=="steel")
                {
                    if(final==Side.Heads)effects.Add(new Effect(EffectType.Score,b.Amount > 0 ? b.Amount : 3));
                    else if(final==Side.Tails)effects.Add(new Effect(EffectType.Penalty,b.PenaltyAmount > 0 ? b.PenaltyAmount : 2));
                }
                else if(b.Kind=="blood"){if(final==Side.Heads||final==Side.Tie)effects.Add(new Effect(EffectType.Score,final==Side.Tie?4:8));if(final==Side.Tails||final==Side.Tie)effects.Add(new Effect(EffectType.Penalty,final==Side.Tie?2:4));}
                else if(b.Kind=="chaos"){int count=effects.Count;for(int i=0;i<count;i++)effects.Add(effects[i].Copy());}
                else if(b.Kind=="rhythm"){if(comboBroke)effects.Add(new Effect(EffectType.Penalty,5));else{double points=Math.Min(8,2*Math.Max(0,g.Encounter.ComboLen-1));if(points>0)effects.Add(new Effect(EffectType.Score,points));}}
                if (b.Kind != "steel" || final != Side.Tie)
                    Signal.Emit(GameSignal.BuffApplied, new GameEvent { Game = g, Inst = item, Buff = b });
            }
        }

        public static double Rule(GameState game, string key, double fallback)
        {
            double value=fallback;
            for(int i=0;i<Math.Min(game.Stake,Stakes.Count);i++) if(Stakes[i].Rules.TryGetValue(key,out var v)) value=v;
            return value;
        }

        public static int Price(GameState game, double value) => (int)Math.Floor(value*Rule(game,"price_mult",1)+.5);

        static double PrintedNet(IReadOnlyList<Effect> effects){double n=0;foreach(var e in effects){if(e.Type==EffectType.Score)n+=e.Amount;else if(e.Type==EffectType.Penalty)n-=e.Amount;}return n;}

        static void ApplyModifier(GameState game)
        {
            if (game.Sandbox != null) return;
            if(game.EncounterIndex<(int)Rule(game,"modifiers_from",2))return;
            string id=ModifierOrder[Rng.Int(game,1,ModifierOrder.Count)-1];game.Encounter.Modifier=id;Modifiers[id].Apply(game,game.Encounter);
        }

        static IEnumerable<AugmentDef> ActiveAugments(GameState game)
        {
            foreach (var id in game.Augments) yield return AugmentDefs[id];
        }

        static void TriggerRunHook(GameState g, RunHookEvent evt)
        {
            var e=g.Encounter;
            if(evt==RunHookEvent.RunStart)g.RunEncounter?.RunStart?.Invoke(g);
            if(evt==RunHookEvent.EncounterStart&&e!=null)g.RunEncounter?.EncounterStart?.Invoke(g,e);
            foreach (var augment in ActiveAugments(g)) augment.OnRunHook(g, evt);
        }

        static List<string> AugmentPool(GameState g)
        {
            var pool = new List<string>();
            foreach (var augment in AugmentCatalog.Ordered)
                if (!g.Augments.Contains(augment.Id) && augment.Available(g)) pool.Add(augment.Id);
            return pool;
        }
        public static bool OfferAugment(GameState g,int level)
        {
            if (g.Phase != Phase.Shop || g.Sandbox != null || (level != 3 && level != 6)) return false;
            var pool = AugmentPool(g);
            if (pool.Count < 3) return false;
            g.AugmentLevel = level;
            g.AugmentOptions = Offers(g, pool, 3);
            g.Phase = Phase.Augment;
            return true;
        }
        public static bool ChooseAugment(GameState g, string id)
        {
            if (g.Phase != Phase.Augment || g.AugmentPending != null || g.AugmentOptions == null ||
                !g.AugmentOptions.Contains(id)) return false;
            var augment = AugmentDefs[id];
            g.Augments.Add(id);
            g.AugmentOptions = null;
            Log(g, "Chosen augment: " + augment.Name + ".");
            g.AugmentPending = augment.OnChosen(g);
            if (g.AugmentPending == null) FinishAugment(g);
            return true;
        }
        static void FinishAugment(GameState g){if(g.Map!=null&&!g.Endless){g.AugmentLevel=null;g.AugmentOptions=null;g.AugmentPending=null;g.Phase=Phase.Map;return;}int level=g.AugmentLevel??g.EncounterIndex+1;g.AugmentLevel=null;g.AugmentOptions=null;g.AugmentPending=null;g.EncounterIndex=level;StartEncounter(g);}
        public static List<AugmentChoice> AugmentChoices(GameState g)
        {
            var pending = g.AugmentPending;
            return g.Phase == Phase.Augment && pending != null
                ? AugmentDefs[pending.Id].Choices(g, pending) : new List<AugmentChoice>();
        }
        public static bool ChooseAugmentOption(GameState g, string key)
        {
            var pending = g.AugmentPending;
            if (pending == null) return false;
            AugmentChoice chosen = null;
            foreach (var choice in AugmentChoices(g)) if (choice.Key == key) chosen = choice;
            if (chosen == null) return false;
            AugmentDefs[pending.Id].ApplyChoice(g, pending, chosen);
            FinishAugment(g);
            return true;
        }

        static double ComboPot(double len)=>Math.Max(0,(len-1)*len/2);
        public static bool CanBankCombo(GameState g)=>g!=null&&g.Phase==Phase.Encounter&&g.Encounter!=null&&g.Pending==null&&g.Encounter.ComboPot>0;
        static int BankComboPot(GameState g,bool counts=true){var e=g.Encounter;int amount=(int)Math.Floor(e.ComboPot);if(amount<=0)return 0;amount+=g.RunEncounter?.ComboBankBonus??0;foreach(var augment in ActiveAugments(g))amount+=augment.BankBonus(g);g.Player.Gold+=amount;if(counts)e.ComboBanked=true;e.ComboPot=0;e.ComboSide=null;e.ComboLen=0;Log(g,"Banked "+amount+" combo gold.");return amount;}
        public static int BankCombo(GameState g)=>CanBankCombo(g)?BankComboPot(g):0;
        public static bool DiscardBank(GameState g,int uid){var e=g.Encounter;if(e==null||e.BankDiscards<1||!e.Hand.Contains(uid)||Discard(g,new[]{uid})==0)return false;e.BankDiscards--;return true;}
        public static int CoinOfferCost(GameState g,int index){if(index<0||index>=g.ShopOffers.Count||g.ShopOffers[index]==null)return -1;var coin=g.ShopOffers[index];int discount=g.RunEncounter?.CoinDiscount?.Invoke(g,coin)??0;return Math.Max(0,Price(g,coin.Cost)-(int)g.Shop.CoinPriceDiscount-(int)CharacterPerkValue(g,CharacterPerkType.CoinDiscount)-discount);}

        static void SetShopStock(GameState g)
        {
            g.ShopOffers=Offers(g,ShopPool(g),g.Shop.CoinOfferCount);
        }
    }
}
