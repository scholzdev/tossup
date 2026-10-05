using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HoroscopeCoin : CoinDef
    {
        public override string Id => "horoscope";
        public override string Name => "Horoscope";
        public override string Description => "Heads: 1 point, all coins +7% Heads this level. Tails: all coins +3%.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.AllOdds(0.07) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.AllOdds(0.03) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
