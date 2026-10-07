using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CrystalBallCoin : CoinDef
    {
        public override string Id => "crystal_ball";
        public override string Name => "Crystal Ball";
        public override string Description => "Heads: 5 points. Tails: discard one of the next three coins.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(5) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.BankDiscard(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Crystal Ball Tails that offer a discard", 10, 35, 100,
            "Heads scores +1 point.", "Tails also gains 1 gold.", "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public CrystalBallCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.BankDiscard) ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Score(1));
                    if (level >= 3) res.Effects.Add(Effect.Score(2));
                }
                else if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Gold(1));
            };
        }
    }
}
