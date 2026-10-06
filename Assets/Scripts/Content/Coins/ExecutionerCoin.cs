using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ExecutionerCoin : CoinDef
    {
        public override string Id => "executioner";
        public override string Name => "Executioner";
        public override string Description => "Heads: 9 points. Tails: add 2 to the quota.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 23;
        public override int EnergyCost => 1;
        public override double Probability => 0.38;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(9) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
