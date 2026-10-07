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
        public OnHooks On { get; } = new OnHooks();

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
        // Hook-driven behavior belongs here; outcome and buff text is generated from its typed data.
        public virtual string SpecialRule => null;
        public virtual IReadOnlyList<BuffSpec> Buffs => Array.Empty<BuffSpec>();
        public virtual string HeadsDescription => null;
        public virtual string TailsDescription => null;
        public virtual string EdgeDescription => null;
        public virtual CoinMastery Mastery => null;

        public IReadOnlyList<Effect> EffectsFor(OutcomeSide side)
        {
            IReadOnlyList<Effect> source = side == OutcomeSide.Heads ? Heads : side == OutcomeSide.Tails ? Tails : Edge;
            var effects = new List<Effect>();
            foreach (var effect in source) effects.Add(effect.Copy());
            return effects;
        }

        public IReadOnlyList<BuffSpec> BuffsFor()
        {
            var buffs = new List<BuffSpec>();
            foreach (var buff in Buffs) buffs.Add(buff.Copy());
            return buffs;
        }

        // Quota estimation adds this to the expected printed effects. Stateful
        // coins own their Lua balance estimates alongside their resolving rules.
        public virtual double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => 0;

        public virtual void OnDeal(GameState game, CoinInst inst) { }
        public virtual void OnDiscard(GameState game, CoinInst inst) { }
        public virtual void OnFlip(GameState game, CoinInst inst, FlipState flip) { }
        public virtual void OnResolve(GameState game, CoinInst inst, Res res) { }
        public virtual void OnOdds(GameState game, CoinInst inst, Odds odds) { }
        public virtual void Grow(CoinInst inst, CoinGrowthEvent evt) { }
        public virtual void Register(CoinCtx ctx) { }
        public bool Overrides(string hook) => GetType().GetMethod(hook).DeclaringType != typeof(CoinDef);
        public bool HasHook(string hook)
        {
            switch (hook)
            {
                case nameof(OnDeal): return On.Coins.HasDeal;
                case nameof(OnDiscard): return On.Coins.HasDiscard;
                case nameof(OnFlip): return On.Coins.HasFlip;
                case nameof(OnResolve): return On.Coins.HasResolve;
                case nameof(OnOdds): return On.Coins.HasOdds;
                case nameof(Grow): return On.Coins.HasGrow;
                case nameof(Register): return Overrides(hook);
                default: return false;
            }
        }

        protected CoinDef()
        {
            // Bridge the existing virtual hooks through the event API while content migrates to +=.
            if (Overrides(nameof(OnOdds))) On.Coins.Odds += (ctx, odds) => OnOdds(ctx.Game, ctx.Coin, odds);
            if (Overrides(nameof(OnDeal))) On.Coins.Deal += ctx => OnDeal(ctx.Game, ctx.Coin);
            if (Overrides(nameof(OnFlip))) On.Coins.Flip += (ctx, flip) => OnFlip(ctx.Game, ctx.Coin, flip);
            if (Overrides(nameof(OnResolve))) On.Coins.Resolve += (ctx, res) => OnResolve(ctx.Game, ctx.Coin, res);
            if (Overrides(nameof(OnDiscard))) On.Coins.Discard += ctx => OnDiscard(ctx.Game, ctx.Coin);
            if (Overrides(nameof(Grow))) On.Coins.Grow += (ctx, evt) => Grow(ctx.Coin, evt);
        }
    }
}
