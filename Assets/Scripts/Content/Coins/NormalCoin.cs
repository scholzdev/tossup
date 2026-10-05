using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class NormalCoin : CoinDef
    {
        public override string Id => "normal";
        public override string Name => "Normal";
        public override string Description => "A plain coin. Barely a scratch.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 5;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.LuckyDay,
            UpgradeCatalog.Mathematician,
        };
    }
}
