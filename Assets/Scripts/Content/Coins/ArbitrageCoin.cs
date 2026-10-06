using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ArbitrageCoin : CoinDef
    {
        public override string Id => "arbitrage";
        public override string Name => "Arbitrage";
        public override string Description => "Heads: gain 3 gold. Tails: score 2 points.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 25;
        public override int EnergyCost => 1;
        public override double Probability => 0.45;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
