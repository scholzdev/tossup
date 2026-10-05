using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CopperCoin : CoinDef
    {
        public override string Id => "copper";
        public override string Name => "Copper";
        public override string Description => "Funds your next move.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.CopperLining,
            UpgradeCatalog.BrightSide,
        };
    }
}
