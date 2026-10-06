using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ConstellationCoin : CoinDef
    {
        public override string Id => "constellation";
        public override string Name => "Constellation";
        public override string Description => "Heads: 2 points. Edge: 4 points. Tails: add 1 to the quota.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 20;
        public override int EnergyCost => 1;
        public override double Probability => 0.35;
        public override double TieProbability => 0.35;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos, CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(1) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(4) };
    }
}
