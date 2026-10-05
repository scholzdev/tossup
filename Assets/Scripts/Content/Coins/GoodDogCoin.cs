using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class GoodDogCoin : CoinDef
    {
        public override string Id => "good_dog";
        public override string Name => "Good Dog";
        public override string Description => "Heads: 2 points; return the highest-scoring coin played this level to the draw pile. Once per level.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 28;
        public override int EnergyCost => 0;
        public override double Probability => 0.43;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.FetchBest(0) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
