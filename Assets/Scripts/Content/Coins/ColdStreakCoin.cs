using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ColdStreakCoin : CoinDef
    {
        public override string Id => "cold_streak";
        public override string Name => "Cold Streak";
        public override string Description => "Tails: 2 points per Tails in a row (max 20). Heads: 1 point.";
        public override string TailsDescription => "Score 2 points per Tails in a row (max 20).";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.2;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => tails * 4;

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Tails) return;
            res.Effects.Add(Effect.Score( Math.Min(20, 2 * game.Encounter.ComboLen)));
        }
    }
}
