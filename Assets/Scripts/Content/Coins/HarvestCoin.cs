using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HarvestCoin : CoinDef
    {
        public override string Id => "harvest";
        public override string Name => "Harvest";
        public override string Description => "Heads: 1 gold and 1 point; the next Greed coin gains 2 gold on Heads. Tails: 1 point.";
        public override string HeadsDescription => "Gain 1 gold, score 1 point, and give the next Greed coin +2 gold on Heads.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 13;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(1), Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("harvest_greed", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Greed), Effect.Gold(2), OutcomeSide.Heads)
        };
    }
}
