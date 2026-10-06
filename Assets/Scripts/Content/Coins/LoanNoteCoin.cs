using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LoanNoteCoin : CoinDef
    {
        public override string Id => "loan_note";
        public override string Name => "Loan Note";
        public override string Description => "Heads: gain 7 gold. Tails: lose 1 gold and add 1 to the quota. Edge: gain 1 gold.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 14;
        public override int EnergyCost => 0;
        public override double Probability => 0.4;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(7) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(1), Effect.Quota(1) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Gold(1) };
    }
}
