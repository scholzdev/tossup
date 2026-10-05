using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LuckyCoin : CoinDef
    {
        public override string Id => "lucky";
        public override string Name => "Lucky";
        public override string Description => "Heads: 2 points, and it goes back into the pile to play again.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.ExtraDraw(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
