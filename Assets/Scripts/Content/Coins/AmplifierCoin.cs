using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class AmplifierCoin : CoinDef
    {
        public override string Id => "amplifier";
        public override string Name => "Amplifier";
        public override string Description => "Heads: 1 point. Active buffs last 1 coin longer; odds and multipliers grow stronger. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.Amplify(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
    }
}
