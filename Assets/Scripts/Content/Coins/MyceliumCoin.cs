using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MyceliumCoin : CoinDef
    {
        public override string Id => "mycelium";
        public override string Name => "Mycelium";
        public override string Description => "Heads: 4 points. Tails: gain 1 energy.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.4;
        public override double TieProbability => 0.2;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Energy generated on Mycelium Tails", 15, 50, 150,
            "Heads scores +1 point.", "Tails scores 1 point.", "Tails grants another energy.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Tails);

        public MyceliumCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.Energy && ctx.Game.Pending?.Result == Side.Tails)
                    ctx.Mastery.Add(Math.Max(0, e.EnergyDelta));
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads && level >= 1) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Tails)
                {
                    if (level >= 2) res.Effects.Add(Effect.Score(1));
                    if (level >= 3) res.Effects.Add(Effect.Energy(1));
                }
            };
        }
    }
}
