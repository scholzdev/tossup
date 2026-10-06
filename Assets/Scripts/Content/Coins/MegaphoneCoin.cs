using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MegaphoneCoin : CoinDef
    {
        public override string Id => "megaphone";
        public override string Name => "Megaphone";
        public override string Description => "Heads: 2 points, and the next 2 coins pay double.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 20;
        public override int EnergyCost => 1;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("megaphone_double_next", OutcomeSide.Heads, BuffTarget.NextCoins(2), Effect.NextMult(2)),
        };
    }
}
