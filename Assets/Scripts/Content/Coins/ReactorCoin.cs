using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ReactorCoin : CoinDef
    {
        public override string Id => "reactor";
        public override string Name => "Reactor";
        public override string Description => "Heads: gain 1 energy and score 1 point. Tails: score 1 point.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 20;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0.05;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel, CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Energy(1), Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
