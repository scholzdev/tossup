using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BloodPriceCoin : CoinDef
    {
        public override string Id => "blood_price";
        public override string Name => "Blood Price";
        public override string Description => "Heads: 4 points. Tails: lose 2 gold.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 18;
        public override int EnergyCost => 1;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood, CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
