using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MomentumCoin : CoinDef
    {
        public override string Id => "momentum";
        public override string Name => "Momentum";
        public override string Description => "+5% Heads for every Heads in a row this level.";
        public override string SpecialRule => "+5% Heads for every Heads in a row this level.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(6) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public MomentumCoin()
        {
            On.Coins.Odds += (ctx, odds) =>
            {
                var encounter = ctx.Game.Encounter;
                if (encounter != null) odds.Heads += .05 * encounter.Streak;
            };
        }
    }
}
