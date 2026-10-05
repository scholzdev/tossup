using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CursedCoin : CoinDef
    {
        public override string Id => "cursed";
        public override string Name => "Cursed";
        public override string Description => "A powerful, dangerous wager.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(15) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
