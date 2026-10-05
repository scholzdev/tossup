using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ConductorCoin : CoinDef
    {
        public override string Id => "conductor";
        public override string Name => "Conductor";
        public override string Description => "Heads: next 3 Rhythm coins gain 2 points per combo step (max 8); a broken combo adds 5 quota.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 24;
        public override int EnergyCost => 0;
        public override double Probability => 0.73;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.TypeBuff(0, 3, CoinType.Rhythm) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
