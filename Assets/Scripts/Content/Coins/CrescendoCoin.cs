using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CrescendoCoin : CoinDef
    {
        public override string Id => "crescendo";
        public override string Name => "Crescendo";
        public override string Description => "Heads: 3 points and extend the combo by 1. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3), Effect.ComboBonus(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
