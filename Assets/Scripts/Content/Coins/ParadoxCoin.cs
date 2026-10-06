using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ParadoxCoin : CoinDef
    {
        public override string Id => "paradox";
        public override string Name => "Paradox";
        public override string Description => "Heads: 3 points. Tails: the next coin uses its other side. Edge: 2 points and the next coin uses its other side.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 26;
        public override int EnergyCost => 1;
        public override double Probability => 0.45;
        public override double TieProbability => 0.2;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.NextSwap() };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(2), Effect.NextSwap() };
    }
}
