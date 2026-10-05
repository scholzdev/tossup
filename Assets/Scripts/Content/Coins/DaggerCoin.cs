using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DaggerCoin : CoinDef
    {
        public override string Id => "dagger";
        public override string Name => "Dagger";
        public override string Description => "Scores either way.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.67;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.DeepCut,
            UpgradeCatalog.SteadyHand,
        };
    }
}
