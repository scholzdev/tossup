using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class RebateCoin : CoinDef
    {
        public override string Id => "rebate";
        public override string Name => "Rebate";
        public override string Description => "Heads: gain 1 gold. Tails: score 2 points. Edge: gain 1 gold.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Gold(1) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Points scored on Rebate Tails", 20, 80, 240,
            "Tails scores +1 point.", "Heads gains +3 gold.", "Edge scores 2 points.",
            MasterySides.Tails, MasterySides.Heads, MasterySides.Edge);

        public RebateCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Res?.Result == Side.Tails) ctx.Mastery.Add(Math.Max(0, e.Flip?.Gained ?? 0));
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Tails && level >= 1) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Heads && level >= 2) res.Effects.Add(Effect.Gold(3));
                if (res.Result == Side.Tie && level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
