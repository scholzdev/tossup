using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class RebateCoin : CoinDef
    {
        public override string Id => "rebate";
        public override string Name => "Rebate";
        public override string Description => "Heads: gain 1 gold. Tails: score 2 points. Edge: gain 1 gold.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Gold(1) };
    }
}
