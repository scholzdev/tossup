using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class WindfallCoin : CoinDef
    {
        public override string Id => "windfall";
        public override string Name => "Windfall";
        public override string Description => "Heads: 6 gold. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 17;
        public override int EnergyCost => 0;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(6) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
