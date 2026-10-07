using System;
using System.Collections.Generic;

namespace Tossup
{
    // A chip (consumable item). Use returns false to refuse; the item is then kept.
    public sealed class ItemDef
    {
        public OnHooks On { get; } = new OnHooks();
        public string Id, Name, Short, Description;
        public int Cost;
        public Func<GameState, bool> Use;
    }

    // A prize (relic): a passive, run-long modifier.
    public sealed class RelicDef
    {
        public OnHooks On { get; } = new OnHooks();
        public string Id, Name, Description;
        public Action<RelicCtx> Register;
    }

    public sealed class LockedCoin
    {
        public CoinDef Coin;
        public string Id => Coin.Id;
        public int Cost; // unused (kept from the original data)

        public LockedCoin(CoinDef coin, int cost)
        {
            Coin = coin;
            Cost = cost;
        }
    }

    // deck: the starting coin set a new player gets. pool: coins usable in coin sets from the start.
    // locked: coins that are unlocked by buying them in the shop. The shop sells every coin of a
    // character, pool and locked alike.
    public sealed class CharacterDef
    {
        public string Id, Name, Description;
        public CoinDef Starter;
        public List<CoinDef> Deck, Pool;
        public List<LockedCoin> Locked = new List<LockedCoin>();
        public List<CharacterPerkDef> Perks = new List<CharacterPerkDef>();
    }

    public sealed class CharacterPerkDef
    {
        public CharacterPerkType Type;
        public string Name, Description;
        public double Value;
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

}
