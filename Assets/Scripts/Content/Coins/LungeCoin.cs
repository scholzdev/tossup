using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LungeCoin : CoinDef
    {
        public override string Id => "lunge";
        public override string Name => "Lunge";
        public override string Description => "Heads: 2 points. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Lunge Heads landed", 30, 120, 360,
            "Heads scores +1.", "Heads scores +2 total.", "Heads scores +3 total.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public LungeCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Flip.Final == Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level)); };
        }
    }
}
