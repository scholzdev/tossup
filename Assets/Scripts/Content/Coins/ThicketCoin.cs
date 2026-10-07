using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ThicketCoin : CoinDef
    {
        public override string Id => "thicket";
        public override string Name => "Thicket";
        public override string Description => "Heads, Tails, or Edge: 1 point.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 11;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(1) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Thicket Edge results", 5, 20, 60,
            "Edge scores +1 point.", "Heads scores +1 point.", "Tails scores +1 point.",
            MasterySides.Edge, MasterySides.Heads, MasterySides.Tails);

        public ThicketCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Res?.Result == Side.Tie) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Tie && level >= 1) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Heads && level >= 2) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Tails && level >= 3) res.Effects.Add(Effect.Score(1));
            };
        }
    }
}
