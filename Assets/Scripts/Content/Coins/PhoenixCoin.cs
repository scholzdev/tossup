using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class PhoenixCoin : CoinDef
    {
        public override string Id => "phoenix";
        public override string Name => "Phoenix";
        public override string Description => "Each Tails stores anger (max 5, or 6 with mastery). Heads: 3 points +2 per anger, or +3 at mastery level 2.";
        public override string SpecialRule => "Each Tails stores anger (max 5, or 6 at mastery level 1).";
        public override string HeadsDescription => "Score 3 points, plus 2 per stored anger (+3 at mastery level 2).";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Anger released by Phoenix", 15, 50, 150,
            "Anger cap rises to 6.", "Each anger scores +1 more point.",
            "Tails adds 1 less quota.",
            MasterySides.None, MasterySides.Heads | MasterySides.Edge, MasterySides.Tails);

        public PhoenixCoin()
        {
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result == Side.Tails)
                {
                    ctx.Coin.Anger = Math.Min(ctx.Mastery.Level >= 1 ? 6 : 5, ctx.Coin.Anger + 1);
                    if (ctx.Mastery.Level >= 3)
                        foreach (var effect in res.Effects)
                            if (effect.Type == EffectType.Penalty) effect.Amount = Math.Max(0, effect.Amount - 1);
                    return;
                }
                double anger = ctx.Coin.Anger;
                if (anger > 0) ctx.Mastery.Add(anger);
                foreach (var effect in res.Effects)
                    if (effect.Type == EffectType.Score)
                        effect.Amount += anger * (ctx.Mastery.Level >= 2 ? 3 : 2);
                ctx.Coin.Anger = 0;
            };
        }

    }
}
