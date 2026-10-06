using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class OmenCoin : CoinDef
    {
        public override string Id => "omen";
        public override string Name => "Omen";
        public override string Description => "Heads: 3 points and the next coin gains 10% Heads chance. Tails: 2 points.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0.15;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3), Effect.NextOdds(0.1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Energy(1) };
    }
}
