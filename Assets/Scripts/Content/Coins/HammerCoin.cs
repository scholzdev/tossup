using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HammerCoin : CoinDef
    {
        public override string Id => "hammer";
        public override string Name => "Hammer";
        public override string Description => "A rare but crushing hit.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 22;
        public override int EnergyCost => 2;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(16) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.HeavyHead,
            UpgradeCatalog.BalancedGrip,
        };
    }
}
