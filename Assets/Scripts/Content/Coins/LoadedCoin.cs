using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LoadedCoin : CoinDef
    {
        public override string Id => "loaded";
        public override string Name => "Loaded";
        public override string Description => "Heads: 4 gold. Tails: lose up to 8 gold. Edge: lose up to 4, then gain 2 gold.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.59;
        public override double TieProbability => 0.23;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune, CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(8) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Loaded losing outcomes endured", 12, 40, 120,
            "Heads grants +3 gold.", "Losing outcomes cost 1 less gold.", "Heads chance +8%.",
            MasterySides.Heads, MasterySides.None, MasterySides.Heads);

        public LoadedCoin()
        {
            On.Game.Coins.Resolved += (ctx,e) => { if(e.Inst==ctx.Coin && e.Res.Result!=Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Heads && ctx.Mastery.Level>=1) res.Effects.Add(Effect.Gold(3)); if(res.Result!=Side.Heads && ctx.Mastery.Level>=2) foreach(var effect in res.Effects) if(effect.Type==EffectType.GoldLoss) effect.Amount=Math.Max(0,effect.Amount-1); };
            On.Coins.Odds += (ctx,odds) => { if(ctx.Mastery.Level>=3) odds.Heads+=.08; };
        }
    }
}
