using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SpareCoilCoin : CoinDef
    {
        public override string Id => "spare_coil";
        public override string Name => "Spare Coil";
        public override string Description => "Heads: gain 1 energy. Tails: score 1 point.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.55;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Energy earned from Spare Coil Heads", 20, 80, 240,
            "Heads also scores 1 point.", "Heads also scores 2 points total.", "Heads grants +1 extra energy.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public SpareCoilCoin()
        {
            On.Game.Effects.Applied += (ctx, e) => { if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.Energy && e.EnergyDelta > 0) ctx.Mastery.Add(e.EnergyDelta); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result != Side.Heads) return; if (ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(Math.Min(2, ctx.Mastery.Level))); if (ctx.Mastery.Level >= 3) res.Effects.Add(Effect.Energy(1)); };
        }
    }
}
