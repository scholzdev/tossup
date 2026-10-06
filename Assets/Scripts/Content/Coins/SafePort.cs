using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SafePort : CoinDef
    {
        public override string Id => "safeport";
        public override string Name => "Safe Port";
        public override string Description => string.Empty;
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 18;
        public override int EnergyCost => 1;
        public override double Probability => 1.0;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge { get; } = Array.Empty<Effect>();
    }
}
