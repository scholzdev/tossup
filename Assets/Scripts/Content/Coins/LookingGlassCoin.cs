using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LookingGlassCoin : CoinDef
    {
        public override string Id => "looking_glass";
        public override string Name => "Looking Glass";
        public override string Description => "Heads: the next coin lands Heads. Tails: the next coin uses its other side. Edge: 2 points.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 25;
        public override int EnergyCost => 0;
        public override double Probability => 0.45;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.NextHeads() };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.NextSwap() };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(3) };
    }
}
