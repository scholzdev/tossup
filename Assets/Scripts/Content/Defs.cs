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
        public List<Effect> Heads = new List<Effect>(), Tails = new List<Effect>();
        public Action<GameState, CoinInst> OnDeal, OnDiscard;
        public Action<GameState, CoinInst, FlipState> OnFlip;
        public Action<GameState, CoinInst, Res> OnResolve;
        public Action<GameState, CoinInst, Odds> OnOdds;
        public Action<CoinInst, string> Grow;
        public Action<CoinCtx> Register;
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
}
