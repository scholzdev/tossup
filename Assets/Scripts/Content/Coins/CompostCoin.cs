using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CompostCoin : CoinDef
    {
        public override string Id => "compost";
        public override string Name => "Compost";
        public override string Description => "Tails: quota +2; once per level, Fortune coins gain +11% Heads for the run (max +55%).";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.47;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2), Effect.FortuneOdds(0.11) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
