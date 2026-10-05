using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BloodPactCoin : CoinDef
    {
        public override string Id => "blood_pact";
        public override string Name => "Blood Pact";
        public override string Description => "Heads: next Blood coin gains 8 points on Heads or adds 4 quota on Tails. Edge gets half of both.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 32;
        public override int EnergyCost => 0;
        public override double Probability => 0.58;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.TypeBuff(0, 1, CoinType.Blood) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
