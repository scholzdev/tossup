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
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Momentum Heads at a streak of three or more", 10, 35, 100,
            "Streak odds rise to +6% per step.", "Heads scores +1 point.",
            "Streak odds rise to +7% per step.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public MomentumCoin()
        {
            On.Coins.Odds += (ctx, odds) =>
            {
                var encounter = ctx.Game.Encounter;
                if (encounter != null)
                    odds.Heads += (ctx.Mastery.Level >= 3 ? .07 : ctx.Mastery.Level >= 1 ? .06 : .05) * encounter.Streak;
            };
            On.Game.Coins.Resolved += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Res.Result == Side.Heads && e.Flip?.Combo?.Len >= 3)
                    ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            { if (res.Result == Side.Heads && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Score(1)); };
        }
    }
}
