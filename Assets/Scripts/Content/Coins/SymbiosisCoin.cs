using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SymbiosisCoin : CoinDef
    {
        public override string Id => "symbiosis";
        public override string Name => "Symbiosis";
        public override string Description => "Heads: 3 points; the next Fortune coin scores +2 on Heads. Tails: gain 1 energy.";
        public override string HeadsDescription => "Score 3 points and buff the next Fortune coin: +2 points on Heads.";
        public override string TailsDescription => "Gain 1 energy.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 20;
        public override int EnergyCost => 1;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("symbiosis", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Fortune), Effect.Score(2), OutcomeSide.Heads)
        };
    }
}
