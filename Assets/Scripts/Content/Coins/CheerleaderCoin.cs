using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CheerleaderCoin : CoinDef
    {
        public override string Id => "cheerleader";
        public override string Name => "Cheerleader";
        public override string Description => "Heads: 2 points, next 2 coins +20% Heads. Tails: next coin +20%.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("cheerleader_heads_odds", OutcomeSide.Heads, BuffTarget.NextCoins(2), Effect.NextOdds(.2)),
            new BuffSpec("cheerleader_tails_odds", OutcomeSide.Tails, BuffTarget.NextCoins(), Effect.NextOdds(.2)),
        };
    }
}
