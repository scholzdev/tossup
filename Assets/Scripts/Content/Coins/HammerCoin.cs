using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HammerCoin : CoinDef
    {
        public override string Id => "hammer";
        public override string Name => "Hammer";
        public override string Description => "A rare but crushing hit.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 22;
        public override int EnergyCost => 2;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(16) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Hammer Heads landed", 10, 35, 100,
            "Heads scores +2 points.", "Heads chance +3%.", "Tails scores +1 point.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Tails);

        public HammerCoin()
        {
            On.Game.Coins.Resolved += (ctx,e) => { if(e.Inst==ctx.Coin && e.Res.Result==Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Heads && ctx.Mastery.Level>=1) res.Effects.Add(Effect.Score(2)); if(res.Result==Side.Tails && ctx.Mastery.Level>=3) res.Effects.Add(Effect.Score(1)); };
            On.Coins.Odds += (ctx,odds) => { if(ctx.Mastery.Level>=2) odds.Heads+=.03; };
        }
    }
}
