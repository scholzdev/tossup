using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LookingGlassCoin : CoinDef
    {
        public override string Id => "looking_glass";
        public override string Name => "Looking Glass";
        public override string Description => "Heads: the next coin lands Heads. Tails: the next coin uses its other side. Edge: 2 points.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 25;
        public override int EnergyCost => 0;
        public override double Probability => 0.45;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.NextHeads() };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.NextSwap() };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(3) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins redirected by Looking Glass", 10, 40, 120,
            "Heads scores 1 point.", "Tails scores 1 point.", "Edge scores +2 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Edge);

        public LookingGlassCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) =>
            {
                if (e.Buff?.SourceUid == ctx.Coin.Uid) ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads && level >= 1) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Tie && level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
