using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SwordCoin : CoinDef
    {
        public override string Id => "sword";
        public override string Name => "Sword";
        public override string Description => "Steady points on Heads.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Heads landed with Sword", 20, 80, 240,
            "Heads scores +1 point.",
            "Tails scores 1 point.",
            "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public SwordCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Flip?.Final == Side.Heads) ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Score(1));
                    if (level >= 3) res.Effects.Add(Effect.Score(2));
                }
                else if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Score(1));
            };
        }
    }
}
