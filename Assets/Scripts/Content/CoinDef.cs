using System;
using System.Collections.Generic;

namespace Tossup
{
    public enum CoinGrowthEvent
    {
        Level,
        Flip,
        Discard,
    }

    // Shared definitions are read-only data and virtual rule hooks. Per-run state
    // belongs to CoinInst; resolving always copies effects before editing them.
    public abstract class CoinDef
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract Rarity Rarity { get; }
        public abstract int Cost { get; }
        public abstract int EnergyCost { get; }
        public abstract double Probability { get; }
        public abstract double TieProbability { get; }
        public abstract IReadOnlyList<CoinType> Types { get; }
        public abstract IReadOnlyList<Effect> Heads { get; }
        public abstract IReadOnlyList<Effect> Tails { get; }
        public abstract IReadOnlyList<Effect> Edge { get; }
        public virtual string HeadsDescription => null;
        public virtual IReadOnlyList<Upgrade> Upgrades => Array.Empty<Upgrade>();

        // Quota estimation adds this to the expected printed effects. Stateful
        // coins own their Lua balance estimates alongside their resolving rules.
        public virtual double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => 0;

        public bool TryGetUpgrade(string id, out Upgrade upgrade)
        {
            foreach(var candidate in Upgrades)if(candidate.Id==id){upgrade=candidate;return true;}
            upgrade=null;return false;
        }
        public virtual void OnDeal(GameState game, CoinInst inst) { }
        public virtual void OnDiscard(GameState game, CoinInst inst) { }
        public virtual void OnFlip(GameState game, CoinInst inst, FlipState flip) { }
        public virtual void OnResolve(GameState game, CoinInst inst, Res res) { }
        public virtual void OnOdds(GameState game, CoinInst inst, Odds odds) { }
        public virtual void Grow(CoinInst inst, CoinGrowthEvent evt) { }
        public virtual void Register(CoinCtx ctx) { }
        public bool Overrides(string hook) => GetType().GetMethod(hook).DeclaringType != typeof(CoinDef);
    }
}
