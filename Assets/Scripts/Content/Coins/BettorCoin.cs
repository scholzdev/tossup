using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BettorCoin : CoinDef
    {
        public override string Id => "bettor";
        public override string Name => "Bettor";
        public override string Description => "Heads: 3 points per flip in the current combo (max 30). Tails: quota +2.";
        public override string HeadsDescription => "Score 3 points per flip in the current combo (max 30).";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Bettor Heads at combo length 3 or more", 10, 35, 100,
            "Those Heads score +2 points.", "Tails adds 1 less quota.", "Heads scores 4 points per combo step (max 36).",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public BettorCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Res?.Result == Side.Heads && e.Flip?.Combo?.Len >= 3) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result == Side.Heads && ctx.Mastery.Level >= 1) res.Effects.Add(Effect.Score(2));
                if (res.Result == Side.Tails && ctx.Mastery.Level >= 2)
                    foreach (var effect in res.Effects) if (effect.Type == EffectType.Penalty) effect.Amount = Math.Max(0, effect.Amount - 1);
            };
        }

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * 6;

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            int step = Profile.MasteryLevel(game.MasteryProfile, this) >= 3 ? 4 : 3;
            res.Effects.Add(Effect.Score(Math.Min(step == 4 ? 36 : 30, step * game.Encounter.ComboLen)));
        }
    }
}
