using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FeintCoin : CoinDef
    {
        public override string Id => "feint";
        public override string Name => "Feint";
        public override string Description => "Heads: 1 point and the next coin uses its other side. Tails: 3 points.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.NextSwap() };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(2) };
    }
}
