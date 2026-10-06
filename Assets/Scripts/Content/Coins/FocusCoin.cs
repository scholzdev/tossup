using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FocusCoin : CoinDef
    {
        public override string Id => "focus";
        public override string Name => "Focus";
        public override string Description => "Heads: the next coin gets +35% Heads. Tails: 4 points.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("focus_next_odds", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.NextOdds(.35)),
        };
        public override IReadOnlyList<Upgrade> Upgrades { get; } = new Upgrade[]
        {
            UpgradeCatalog.ClearMind,
            UpgradeCatalog.FollowThrough,
        };
    }
}
