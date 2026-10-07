using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ArbitrageCoin : CoinDef
    {
        public override string Id => "arbitrage";
        public override string Name => "Arbitrage";
        public override string Description => "Heads: gain 3 gold. Tails: score 2 points.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 25;
        public override int EnergyCost => 1;
        public override double Probability => 0.45;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Gold earned on Arbitrage Heads", 30, 120, 360,
            "Tails scores +1 point.", "Heads gains +3 gold.", "Heads also scores 2 points.",
            MasterySides.Tails, MasterySides.Heads, MasterySides.Heads);

        public ArbitrageCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.Gold) ctx.Mastery.Add(Math.Max(0, e.GoldDelta));
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Tails && level >= 1) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Heads)
                {
                    if (level >= 2) res.Effects.Add(Effect.Gold(3));
                    if (level >= 3) res.Effects.Add(Effect.Score(2));
                }
            };
        }
    }
}
