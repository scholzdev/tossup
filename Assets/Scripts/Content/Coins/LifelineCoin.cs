using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LifelineCoin : CoinDef
    {
        public override string Id => "lifeline";
        public override string Name => "Lifeline";
        public override string Description => "Heads: 1 point, and draw one more coin into your hand.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.DrawCoin(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins drawn by Lifeline", 10, 35, 100,
            "Heads scores +1 point.", "Heads draws another coin.", "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public LifelineCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.DrawCoin) ctx.Mastery.Add(e.Effect.Amount);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads) return;
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(1));
                if (level >= 2) res.Effects.Add(Effect.DrawCoin(1));
                if (level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
