using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CopperCoin : CoinDef
    {
        public override string Id => "copper";
        public override string Name => "Copper";
        public override string Description => "Funds your next move.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Gold earned by Copper", 20, 80, 240,
            "Heads grants +3 gold.",
            "Tails grants +1 energy.",
            "Heads also scores 2 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public CopperCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.GoldDelta > 0) ctx.Mastery.Add(e.GoldDelta);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Gold(3));
                    if (level >= 3) res.Effects.Add(Effect.Score(2));
                }
                else if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Energy(1));
            };
        }
    }
}
