using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DoppelgangerCoin : CoinDef
    {
        public override string Id => "doppelganger";
        public override string Name => "Doppelganger";
        public override string Description => "Heads: next Chaos coin applies its resolved effects twice, including penalties.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 34;
        public override int EnergyCost => 0;
        public override double Probability => 0.53;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.TypeBuff(0, 1, CoinType.Chaos) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
