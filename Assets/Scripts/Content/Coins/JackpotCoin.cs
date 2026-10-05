using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class JackpotCoin : CoinDef
    {
        public override string Id => "jackpot";
        public override string Name => "Jackpot";
        public override string Description => "Heads: 25 points. Only 15% Heads.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 18;
        public override int EnergyCost => 1;
        public override double Probability => 0.15;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(25) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
