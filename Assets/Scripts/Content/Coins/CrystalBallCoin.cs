using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CrystalBallCoin : CoinDef
    {
        public override string Id => "crystal_ball";
        public override string Name => "Crystal Ball";
        public override string Description => "Heads: 5 points. Tails: discard one of the next three coins.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(5) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.BankDiscard(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
