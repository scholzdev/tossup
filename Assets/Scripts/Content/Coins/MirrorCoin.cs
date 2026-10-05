using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MirrorCoin : CoinDef
    {
        public override string Id => "mirror";
        public override string Name => "Mirror";
        public override string Description => "Heads: the next coin uses the effects of its other side. Tails: 2 points.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.NextSwap(0, 1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
