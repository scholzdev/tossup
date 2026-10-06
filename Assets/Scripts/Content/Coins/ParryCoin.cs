using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ParryCoin : CoinDef
    {
        public override string Id => "parry";
        public override string Name => "Parry";
        public override string Description => "Heads: 1 point and a combo shield. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.55;
        public override double TieProbability => 0.05;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.ComboShield(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
