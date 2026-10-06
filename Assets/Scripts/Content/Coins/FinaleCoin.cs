using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FinaleCoin : CoinDef
    {
        public override string Id => "finale";
        public override string Name => "Finale";
        public override string Description => "Heads: 4 points and add 2 combo steps. Tails: gain 2 gold.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 25;
        public override int EnergyCost => 1;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4), Effect.ComboBonus(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Gold(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
