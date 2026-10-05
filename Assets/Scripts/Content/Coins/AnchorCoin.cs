using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class AnchorCoin : CoinDef
    {
        public override string Id => "anchor";
        public override string Name => "Anchor";
        public override string Description => "Heads: 2 points, and the next time the combo would break it holds instead.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.ComboShield(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
