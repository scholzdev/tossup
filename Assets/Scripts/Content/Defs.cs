using System;
using System.Collections.Generic;

namespace Tossup
{
    // A coin: data (name, description, rarity N/R/SR/UR, Heads probability, Heads and Tails effect lists,
    // shop cost, energy cost to flip) plus optional hooks (see Core/Hooks.cs).
    public sealed class CoinDef
    {
        public string Id, Name, Description, Rarity;
        public double Probability;
        public int? Cost; // shop price; null means 15
        public int EnergyCost;
        public double TieProbability;
        public List<string> CoinTypes = new List<string>();
        public Dictionary<string, CoinUpgradeDef> Upgrades = new Dictionary<string, CoinUpgradeDef>();
        public List<Effect> Heads = new List<Effect>(), Tails = new List<Effect>();
        public Action<GameState, CoinInst> OnDeal, OnDiscard;
        public Action<GameState, CoinInst, FlipState> OnFlip;
        public Action<GameState, CoinInst, Res> OnResolve;
        public Action<GameState, CoinInst, Odds> OnOdds;
        public Action<CoinInst, string> Grow;
        public Action<CoinCtx> Register;
    }

    public sealed class CoinUpgradeDef
    {
        public string Id, Name, Description;
        public int Cost;
        public double HeadsScore, HeadsProbability;
    }

    // A chip (consumable item). Use returns false to refuse; the item is then kept.
    public sealed class ItemDef
    {
        public string Id, Name, Short, Description;
        public int Cost;
        public Func<GameState, bool> Use;
    }

    // A prize (relic): a passive, run-long modifier.
    public sealed class RelicDef
    {
        public string Id, Name, Description;
        public Action<RelicCtx> Register;
    }

    public sealed class LockedCoin
    {
        public string Id;
        public int Cost; // unused (kept from the original data)

        public LockedCoin(string id, int cost)
        {
            Id = id;
            Cost = cost;
        }
    }

    // deck: the starting coin set a new player gets. pool: coins usable in coin sets from the start.
    // locked: coins that are unlocked by buying them in the shop. The shop sells every coin of a
    // character, pool and locked alike.
    public sealed class CharacterDef
    {
        public string Id, Name, Description, Starter;
        public List<string> Deck, Pool;
        public List<LockedCoin> Locked = new List<LockedCoin>();
    }

    public sealed class ModifierDef
    {
        public string Id, Name, Description;
        public Action<GameState, Encounter> Apply;
    }

    public sealed class StakeDef
    {
        public string Text, Info;
        public Dictionary<string, double> Rules = new Dictionary<string, double>();
    }

    public sealed class ContractDef
    {
        public string Id, Name, Description, Drawback, RewardText;
        public int Reward;
        public double HeadsPenalty;
        public Action<GameState, Encounter> Apply;
        public Func<GameState, Encounter, bool> Complete;
        public Action<GameState, Encounter> RewardAction;
    }

    public sealed class RunEncounterDef
    {
        public string Id, Name, Description;
        public Action<GameState, string> Trigger;
    }

    public sealed class AugmentDef
    {
        public string Id, Name, Description, Tier;
        public Action<GameState, string> Trigger;
        public Func<GameState, AugmentPending> Choose;
        public Func<GameState, AugmentPending, List<AugmentChoice>> Choices;
        public Func<GameState, AugmentPending, string, bool> ApplyChoice;
    }
}
