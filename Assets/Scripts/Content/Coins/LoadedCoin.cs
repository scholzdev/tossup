using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LoadedCoin : CoinDef
    {
        public override string Id => "loaded";
        public override string Name => "Loaded";
        public override string Description => "Heads: 4 gold. Tails: lose up to 8 gold. Edge: lose up to 4, then gain 2 gold.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.59;
        public override double TieProbability => 0.23;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune, CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(8) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.SaferBet,
            UpgradeCatalog.GildedFace,
        };
    }
}
