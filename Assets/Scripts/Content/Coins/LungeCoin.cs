using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LungeCoin : CoinDef
    {
        public override string Id => "lunge";
        public override string Name => "Lunge";
        public override string Description => "Heads: 2 points. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
