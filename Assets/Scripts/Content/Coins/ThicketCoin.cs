using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ThicketCoin : CoinDef
    {
        public override string Id => "thicket";
        public override string Name => "Thicket";
        public override string Description => "Heads, Tails, or Edge: 1 point.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 11;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(1) };
    }
}
