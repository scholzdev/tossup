using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BloodlettingCoin : CoinDef
    {
        public override string Id => "bloodletting";
        public override string Name => "Bloodletting";
        public override string Description => "Heads: 5 points. Tails: lose 1 gold.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 19;
        public override int EnergyCost => 1;
        public override double Probability => 0.45;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(5) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
