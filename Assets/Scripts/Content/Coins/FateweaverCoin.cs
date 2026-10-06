using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FateweaverCoin : CoinDef
    {
        public override string Id => "fateweaver";
        public override string Name => "Fateweaver";
        public override string Description => "Heads: 2 points and the next Fortune coin scores +2 on Heads. Tails: 1 point.";
        public override string HeadsDescription => "Score 2 points and buff the next Fortune coin: +2 points on Heads.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 21;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("fateweaver", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Fortune), Effect.Score(2), OutcomeSide.Heads)
        };
    }
}
