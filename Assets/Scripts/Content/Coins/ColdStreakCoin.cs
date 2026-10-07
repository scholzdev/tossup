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
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Cold Streak Tails at streak length 2 or more", 10, 40, 120,
            "Tails scores +2 points.", "Its Tails payout cap rises to 24.", "Heads scores +2 points.",
            MasterySides.Tails, MasterySides.Tails, MasterySides.Heads);

        public ColdStreakCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Res?.Result == Side.Tails && e.Flip?.Combo?.Len >= 2) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Tails && level >= 1) res.Effects.Add(Effect.Score(2));
                if (res.Result == Side.Heads && level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => tails * 4;

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Tails) return;
            int cap = Profile.MasteryLevel(game.MasteryProfile, this) >= 2 ? 24 : 20;
            res.Effects.Add(Effect.Score(Math.Min(cap, 2 * game.Encounter.ComboLen)));
        }
    }
}
