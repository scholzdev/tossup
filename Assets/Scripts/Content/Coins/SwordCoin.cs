using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SwordCoin : CoinDef
    {
        public override string Id => "sword";
        public override string Name => "Sword";
        public override string Description => "Steady points on Heads.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.KeenEdge,
            UpgradeCatalog.TrueAim,
        };
    }
}
