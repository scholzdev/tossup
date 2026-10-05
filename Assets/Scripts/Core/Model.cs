using System;
using System.Collections.Generic;

namespace Tossup
{
    public enum Phase { Encounter, Shop, Victory, GameOver }

    public static class Side
    {
        public const string Heads = "Heads", Tails = "Tails";
        public static string Other(string side) => side == Heads ? Tails : Heads;
    }

    // An effect of a coin side: {type, amount, coins}. Types: score, gold, energy, penalty, probability,
    // extra_draw, amplify, combo_bonus, combo_shield, next_mult, next_odds, next_swap, next_heads.
    public sealed class Effect
    {
        public string Type;
        public double Amount;
        public int? Coins; // "next N coins" effects; null means 1

        public Effect(string type, double amount = 0, int? coins = null)
        {
            Type = type;
            Amount = amount;
            Coins = coins;
        }

        public Effect Copy() => new Effect(Type, Amount, Coins);
    }

    // An owned coin. It lives for the whole run, so a coin's own counters are how it scales itself.
    public sealed class CoinInst
    {
        public int Uid;
        public string Id;
        public double Bonus; // odds tuner upgrades
        public double Charge; // Fuse
        public bool Jackpot; // Lucky Seven
        public double Debt; // Martyr
        public double Stack; // Snowball
        public double Anger; // Phoenix
    }

    // A "next N coins ..." buff. Kind: mult, odds, swap, heads. A buff made while a coin resolves is
    // fresh and starts with the following coin.
    public sealed class Buff
    {
        public string Kind;
        public double Amount;
        public int Left;
        public bool Fresh;
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
        public double Probability;
        public string Raw, Result, Final;
        public bool Forced;
        public string Altered; // "BUFF", "RELIC", "THE HOUSE": shown so a changed side is never a mystery
        public List<Effect> BaseEffects;
        public Combo Combo;
        public double? Gained, Penalty;
    }

    // What a resolving coin is about to do; hooks may edit, add to or replace Effects.
    public sealed class Res
    {
        public string Result, Raw;
        public List<Effect> Effects = new List<Effect>();
        public bool CashOut;
    }

    public sealed class Odds
    {
        public double P;
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
        public List<string> ShopOffers = new List<string>(); // null entries are sold
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
        // presentation flags kept on the run, as the original did
        public bool Paused;
        public int? TokensPaid;
        public bool EndlessLogged;
    }

    public sealed class GameRuleException : Exception
    {
        public GameRuleException(string message) : base(message) { }
    }
}
