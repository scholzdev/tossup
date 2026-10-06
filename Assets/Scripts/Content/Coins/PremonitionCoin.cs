using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class PremonitionCoin : CoinDef
    {
        public override string Id => "premonition";
        public override string Name => "Premonition";
        public override string Description => "Heads: the next coin lands Heads. Tails: 3 points. Edge: gain 1 energy.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.55;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.NextHeads() };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Energy(1) };
    }
}
