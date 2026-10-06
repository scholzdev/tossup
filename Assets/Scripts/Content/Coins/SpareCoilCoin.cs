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
    }
}
