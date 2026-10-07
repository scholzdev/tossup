using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class GoodDogCoin : CoinDef
    {
        public override string Id => "good_dog";
        public override string Name => "Good Dog";
        public override string Description => "Heads: 2 points; return the highest-scoring coin played this level to the draw pile. Once per level.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 28;
        public override int EnergyCost => 0;
        public override double Probability => 0.43;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.FetchBest(0) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins fetched by Good Dog", 5, 20, 60,
            "Heads scores +1 point.", "Heads gains 1 gold.", "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public GoodDogCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.FetchBest && e.ReturnedDelta > 0)
                    ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads) return;
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(1));
                if (level >= 2) res.Effects.Add(Effect.Gold(1));
                if (level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
