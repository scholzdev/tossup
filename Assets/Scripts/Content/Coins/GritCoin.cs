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
    }
}
