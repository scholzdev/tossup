using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class GritCoin : CoinDef
    {
        public override string Id => "grit";
        public override string Name => "Grit";
        public override string Description => "Heads: 1 point. Tails: gain 1 energy.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 11;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0.05;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(1), Effect.Energy(1) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Energy earned from Grit", 20, 80, 240,
            "Grit Heads scores +1.", "Grit Tails scores 1.", "Grit Edge scores +1.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Edge);

        public GritCoin()
        {
            On.Game.Effects.Applied += (ctx, e) => { if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.Energy && e.EnergyDelta > 0) ctx.Mastery.Add(e.EnergyDelta); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level >= 1) res.Effects.Add(Effect.Score(1)); if (res.Result == Side.Tails && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Score(1)); if (res.Result == Side.Tie && ctx.Mastery.Level >= 3) res.Effects.Add(Effect.Score(1)); };
        }
    }
}
