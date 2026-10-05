using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HotHandCoin : CoinDef
    {
        public override string Id => "hot_hand";
        public override string Name => "Hot Hand";
        public override string Description => "Heads: 2 points, and the combo grows by 1 extra step.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.ComboBonus(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
