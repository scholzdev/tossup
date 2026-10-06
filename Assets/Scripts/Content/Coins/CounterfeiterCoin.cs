using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CounterfeiterCoin : CoinDef
    {
        public override string Id => "counterfeiter";
        public override string Name => "Counterfeiter";
        public override string Description => "Heads: next 2 Greed coins double all gold gained. Each Tails also loses up to 3 gold.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 25;
        public override int EnergyCost => 0;
        public override double Probability => 0.61;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(3) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("counterfeiter_greed", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Greed, 2), Effect.TypeBuff(kind: CoinType.Greed)),
        };
    }
}
