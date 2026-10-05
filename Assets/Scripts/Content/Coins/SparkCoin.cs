using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SparkCoin : CoinDef
    {
        public override string Id => "spark";
        public override string Name => "Spark";
        public override string Description => "Energy or a small strike.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 9;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Energy(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.HotSpark,
            UpgradeCatalog.ReliableSpark,
        };
    }
}
