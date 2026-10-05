using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class WhetstoneCoin : CoinDef
    {
        public override string Id => "whetstone";
        public override string Name => "Whetstone";
        public override string Description => "Heads: next 2 Steel coins gain 3 points on Heads, but add 2 quota on Tails.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 22;
        public override int EnergyCost => 0;
        public override double Probability => 0.67;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.TypeBuff(0, 2, CoinType.Steel) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
