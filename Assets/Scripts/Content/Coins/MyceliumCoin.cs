using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MyceliumCoin : CoinDef
    {
        public override string Id => "mycelium";
        public override string Name => "Mycelium";
        public override string Description => "Heads: 4 points. Tails: gain 1 energy.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.4;
        public override double TieProbability => 0.2;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
