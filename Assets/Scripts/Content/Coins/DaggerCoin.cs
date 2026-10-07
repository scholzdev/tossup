using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DaggerCoin : CoinDef
    {
        public override string Id => "dagger";
        public override string Name => "Dagger";
        public override string Description => "Scores either way.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.67;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Dagger Tails landed", 15, 55, 160,
            "Tails scores +1 point.", "Heads chance +2%.", "Heads scores +1 point.",
            MasterySides.Tails, MasterySides.Heads, MasterySides.Heads);

        public DaggerCoin()
        {
            On.Game.Coins.Resolved += (ctx,e) => { if(e.Inst==ctx.Coin && e.Res.Result==Side.Tails) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Tails && ctx.Mastery.Level>=1) res.Effects.Add(Effect.Score(1)); if(res.Result==Side.Heads && ctx.Mastery.Level>=3) res.Effects.Add(Effect.Score(1)); };
            On.Coins.Odds += (ctx,odds) => { if(ctx.Mastery.Level>=2) odds.Heads+=.02; };
        }
    }
}
