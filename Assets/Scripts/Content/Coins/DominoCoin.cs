using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DominoCoin : CoinDef
    {
        public override string Id => "domino";
        public override string Name => "Domino";
        public override string Description => "Heads: 2 points, and the next coin lands Heads. Tails: quota +1.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.NextHeads(0, 1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
