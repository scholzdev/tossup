using System;
using System.Collections.Generic;

namespace Tossup
{
    public enum Phase { Encounter, Contract, Augment, Shop, Victory, GameOver }

    public static class Side
    {
        public const string Heads = "Heads", Tails = "Tails", Tie = "Tie";
         public static string Other(string side) => side == Heads ? Tails : side == Tails ? Heads : Tie;
    }

    // An effect of a coin side: {type, amount, coins}. Types: score, gold, energy, penalty, probability,
    // extra_draw, amplify, combo_bonus, combo_shield, next_mult, next_odds, next_swap, next_heads.
    public sealed class Effect
    {
        public EffectType Type;
        public double Amount;
        public int? Coins; // "next N coins" effects; null means 1
        public CoinType? Kind; // coin type for type_buff

        public Effect() { } // StateJson restores public data fields.

        public Effect(EffectType type, double amount = 0, int? coins = null)
        {
            Type = type;
            Amount = amount;
            Coins = coins;
        }

        public static Effect AllOdds(double amount = 0, int? coins = null) => new Effect(EffectType.AllOdds, amount, coins);
        public static Effect Amplify(double amount = 0, int? coins = null) => new Effect(EffectType.Amplify, amount, coins);
        public static Effect BankDiscard(double amount = 0, int? coins = null) => new Effect(EffectType.BankDiscard, amount, coins);
        public static Effect ComboBonus(double amount = 0, int? coins = null) => new Effect(EffectType.ComboBonus, amount, coins);
        public static Effect ComboShield(double amount = 0, int? coins = null) => new Effect(EffectType.ComboShield, amount, coins);
        public static Effect Energy(double amount = 0, int? coins = null) => new Effect(EffectType.Energy, amount, coins);
        public static Effect ExtraDraw(double amount = 0, int? coins = null) => new Effect(EffectType.ExtraDraw, amount, coins);
        public static Effect ExtraExchange(double amount = 0, int? coins = null) => new Effect(EffectType.ExtraExchange, amount, coins);
        public static Effect FetchBest(double amount = 0, int? coins = null) => new Effect(EffectType.FetchBest, amount, coins);
        public static Effect FortuneOdds(double amount = 0, int? coins = null) => new Effect(EffectType.FortuneOdds, amount, coins);
        public static Effect Gold(double amount = 0, int? coins = null) => new Effect(EffectType.Gold, amount, coins);
        public static Effect GoldLoss(double amount = 0, int? coins = null) => new Effect(EffectType.GoldLoss, amount, coins);
        public static Effect NextHeads(double amount = 0, int? coins = null) => new Effect(EffectType.NextHeads, amount, coins);
        public static Effect NextMult(double amount = 0, int? coins = null) => new Effect(EffectType.NextMult, amount, coins);
        public static Effect NextOdds(double amount = 0, int? coins = null) => new Effect(EffectType.NextOdds, amount, coins);
        public static Effect NextSwap(double amount = 0, int? coins = null) => new Effect(EffectType.NextSwap, amount, coins);
        public static Effect Quota(double amount = 0, int? coins = null) => new Effect(EffectType.Penalty, amount, coins);
        public static Effect Probability(double amount = 0, int? coins = null) => new Effect(EffectType.Probability, amount, coins);
        public static Effect Score(double amount = 0, int? coins = null) => new Effect(EffectType.Score, amount, coins);
        public static Effect TypeBuff(double amount = 0, int? coins = null, CoinType? kind = null) => new Effect(EffectType.TypeBuff, amount, coins) { Kind = kind };
        public static IReadOnlyList<Effect> HalfOf(params IReadOnlyList<Effect>[] sides)
        {
            var result=new List<Effect>();
            foreach(var side in sides)foreach(var effect in side)
                if(effect.Type==EffectType.Score||effect.Type==EffectType.Gold||effect.Type==EffectType.GoldLoss||effect.Type==EffectType.Energy||effect.Type==EffectType.Penalty)
                    result.Add(new Effect(effect.Type,effect.Amount/2));
            return result;
        }

        public Effect Copy() => new Effect(Type, Amount, Coins) { Kind = Kind };

        public Effect WithKind(CoinType kind) { Kind = kind; return this; }
    }

    // An owned coin. It lives for the whole run, so a coin's own counters are how it scales itself.
    public sealed class CoinInst
    {
        public int Uid;
        public CoinDef Definition;
        public string Id => Definition?.Id;
        public double Charge; // Fuse
        public bool Jackpot; // Lucky Seven
        public double Debt; // Martyr
        public double Stack; // Snowball
        public double Anger; // Phoenix
        public int CompostLevel;
        public int FetchedLevel;
        public int MasteryFirstHeadsLevel;
    }

    // A "next N coins ..." buff. Kind: mult, odds, swap, heads. A buff made while a coin resolves is
    // fresh and starts with the following coin.
    public sealed class Buff
    {
        // Kind is retained as a stable save key for the original encounter buffs.
        public string Kind;
        public double Amount;
        public int Left;
        public bool Fresh;
        public string SpecId;
        public CoinType? TargetType;
        public OutcomeSide? AppliesOn;
        public Effect AppliedEffect;
        public int SourceUid;
        public double PenaltyAmount;
    }

    public sealed class Combo
    {
        public double Len;
        public double Mult;
        public string Side;
    }

    // A coin on its way through the stage: dealt ({Uid, Probability}), pending (rolled, not yet resolved)
    // and finally the last result (with Final, Gained, Penalty, Combo).
    public sealed class FlipState
    {
        public int Uid;
        public string CoinId;
        public double Probability;
        public double TieProbability;
        public double OddsSampleTime;
        public string Raw, Result, Final;
        public bool Forced;
        public string Altered; // "BUFF", "RELIC", "THE HOUSE": shown so a changed side is never a mystery
        public List<Effect> BaseEffects;
        public List<BuffSpec> BaseBuffs;
        public Combo Combo;
        public double? Gained, Penalty;
    }

    // What a resolving coin is about to do; hooks may edit, add to or replace Effects.
    public sealed class Res
    {
        public string Result, Raw;
        public List<Effect> Effects = new List<Effect>();
        public List<BuffSpec> Buffs = new List<BuffSpec>();
        public bool CashOut;
    }

    public sealed class Odds
    {
        double heads, edge, tails;
        bool headsTouched, edgeTouched, tailsTouched;

        public double Heads { get => heads; set { heads = value; headsTouched = true; } }
        public double Edge { get => edge; set { edge = value; edgeTouched = true; } }
        public double Tails { get => tails; set { tails = value; tailsTouched = true; } }

        public Odds() { }

        public Odds(double heads, double edge, double tails)
        {
            this.heads = heads;
            this.edge = edge;
            this.tails = tails;
        }

        // A hook changing Heads or Edge alone transfers the difference to/from Tails;
        // changing Tails alone transfers it to/from Heads. Explicit multi-side edits are normalized.
        public void Normalize()
        {
            heads = Valid(heads);
            edge = Valid(edge);
            tails = Valid(tails);

            if ((headsTouched || edgeTouched) && !tailsTouched)
            {
                double total = heads + edge;
                if (total > 1) { heads /= total; edge /= total; }
                tails = 1 - heads - edge;
            }
            else if (tailsTouched && !headsTouched && !edgeTouched)
            {
                tails = Math.Min(tails, 1 - edge);
                heads = 1 - edge - tails;
            }
            else
            {
                double total = heads + edge + tails;
                if (total <= 0) { heads = edge = 0; tails = 1; }
                else { heads /= total; edge /= total; tails /= total; }
            }

            headsTouched = edgeTouched = tailsTouched = false;
        }

        static double Valid(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0 : Math.Max(0, value);
    }

    public sealed class Player
    {
        public double Gold, Energy, MaxEnergy;
    }

    public sealed class Mulligan
    {
        public List<int> Hand = new List<int>();
    }

    public sealed class Encounter
    {
        public string Name;
        public double ElapsedSeconds;
        public double Quota, MaxQuota; // Quota is what is still missing; MaxQuota grows with penalties
        public bool Boss, Inverts;
        public int? Endless;
        public double? Payout;
        public int Flips;
        public double Scored;
        public bool Cleared;
        public double SurplusPaid;
        public HashSet<int> Discarded = new HashSet<int>();
        public int Discards;
        public Dictionary<int, double> Bonus = new Dictionary<int, double>();
        public double Magnet;
        public int Streak;
        public List<Buff> Buffs = new List<Buff>();
        public string ComboSide;
        public double ComboLen;
        public double Shield;
        public double ComboStep, ComboCap;
        public int Returned;
        public List<int> Played = new List<int>();
        public List<int> Pile = new List<int>();
        public List<int> Queue = new List<int>();
        public int Exchanges;
        public int Doubler; // Doubler flips this level
        public int Tails;
        public string Modifier;
        public double GoldMult = 1;
        public int ExtraExchanges;
        public int BankDiscards;
        public double ComboPot;
        public bool ComboBanked;
        public double BestComboLen;
        public Dictionary<int, double> BestScores = new Dictionary<int, double>();
        public HashSet<int> DealHooksFired = new HashSet<int>();
        public ContractState Contract;
        public List<string> ContractOptions;
        public int SideBets;
        public string SideBetSide, SideBetOutcome;
        public int SideBetCost;
        public double SideBetPayout;
        public bool PushAvailable;
        public bool PushUsed;
        public double PushScore;
        public bool AllInPaid;
        public HashSet<int> TypeSpecialistPaid = new HashSet<int>();
    }

    public sealed class ContractState
    {
        public string Id;
        public string Result;
        public double StartGold;
        public int StartDiscards;
    }

    public sealed class ShopState
    {
        public int RefreshCost = 4;
        public int CoinOfferCount = 4;
        public double CoinPriceDiscount;
    }

    public sealed class AugmentPending
    {
        public string Id;
        public string RewardId;
    }

    public sealed class AugmentChoice
    {
        public string Key, Title, Detail, CoinId;
    }

    public sealed class GameState
    {
        public long Seed;
        public long RngState;
        public double? LastRng;
        public string CharacterId;
        public Phase Phase;
        public Player Player = new Player();
        public List<CoinInst> Coins = new List<CoinInst>();
        public List<string> Relics = new List<string>();
        public List<string> Items = new List<string>();
        public List<string> ShopItems = new List<string>(); // null entries are sold
        public List<CoinDef> ShopOffers = new List<CoinDef>(); // null entries are sold
        public string ShopRelic;
        public List<string> Unlocked = new List<string>();
        public HashSet<string> Purchased = new HashSet<string>();
        public int Cleared;
        public int NextUid;
        public int EncounterIndex = 1;
        public Encounter Encounter;
        public FlipState Pending, Dealt, LastResult;
        public Mulligan Mulligan;
        public List<int> Peek;
        public List<string> Log = new List<string>();
        public int? SelectedUid;
        public bool ManualMulligan;
        public bool Reshuffle; // tests / coin simulator only: refill the pile when it runs dry
        public int RerollCost = 4;
        public bool ExchangeOpen;
        public string LostWhy;
        public bool Endless;
        public int Slots;
        public int Stake = 1;
        public double FortuneBonus;
        public int RerollStep = 2;
        public bool ContractsEnabled = true;
        public string RunEncounterId;
        [NonSerialized] public RunEncounterDef RunEncounter;
        public List<string> Augments = new List<string>();
        public Dictionary<string, string> AugmentData = new Dictionary<string, string>();
        public int? AugmentLevel;
        public List<string> AugmentOptions;
        public AugmentPending AugmentPending;
        public double NextLevelQuotaBonus;
        public ShopState Shop = new ShopState();
        // presentation flags kept on the run, as the original did
        public bool Paused;

        public void SetRunEncounter(RunEncounterDef encounter)
        {
            RunEncounter = encounter;
            RunEncounterId = encounter?.Id;
        }
        public int? TokensPaid;
        public bool EndlessLogged, EndlessRecord, OverSeen;
        public bool WinRecorded, EndlessRecorded;
        public string UnlockedCharacter;
        public int? UnlockedStake;
        public bool Tutorial;
        public int TutorialHeads;
        public SandboxConfig Sandbox;
        [NonSerialized] public ProfileData MasteryProfile;
    }

    public sealed class GameRuleException : Exception
    {
        public GameRuleException(string message) : base(message) { }
    }
}
