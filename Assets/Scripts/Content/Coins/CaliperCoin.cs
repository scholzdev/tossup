using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CaliperCoin : CoinDef
    {
        public override string Id => "caliper";
        public override string Name => "Caliper";
        public override string Description => "Heads: 2 points. At 1 energy or less, gain +12% Heads chance.";
        public override string SpecialRule => "While you have 1 energy or less, this coin gains +12% Heads chance.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public CaliperCoin()
        {
            On.Coins.Odds += (context, odds) =>
            {
                if (context.Game.Player.Energy <= 1) odds.Heads += 0.12;
            };
        }
    }
}
