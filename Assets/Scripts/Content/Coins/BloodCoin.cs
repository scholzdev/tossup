using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BloodCoin : CoinDef
    {
        public override string Id => "blood";
        public override string Name => "Blood";
        public override string Description => "Heads: 10 points. Tails: quota +6. Edge: half of both.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.37;
        public override double TieProbability => 0.09;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(10) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(6) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.Bloodletting,
            UpgradeCatalog.SureStrike,
        };
    }
}
