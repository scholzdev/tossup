using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BloodCoin : CoinDef
    {
        public override string Id => "blood";
        public override string Name => "Blood";
        public override string Description => "Heads: 10 points. Tails: quota +6. Edge: half of both.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.37;
        public override double TieProbability => 0.09;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(10) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(6) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Blood Heads landed", 12, 45, 130,
            "Heads scores +1 point.", "Tails adds 1 less quota.", "Heads chance +2%.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public BloodCoin()
        {
            On.Game.Coins.Resolved += (ctx,e) => { if(e.Inst==ctx.Coin && e.Res.Result==Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Heads && ctx.Mastery.Level>=1) res.Effects.Add(Effect.Score(1)); if(res.Result==Side.Tails && ctx.Mastery.Level>=2) foreach(var effect in res.Effects) if(effect.Type==EffectType.Penalty) effect.Amount=Math.Max(0,effect.Amount-1); };
            On.Coins.Odds += (ctx,odds) => { if(ctx.Mastery.Level>=3) odds.Heads+=.02; };
        }
    }
}
