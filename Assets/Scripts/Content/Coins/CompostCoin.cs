using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CompostCoin : CoinDef
    {
        public override string Id => "compost";
        public override string Name => "Compost";
        public override string Description => "Tails: quota +2; once per level, Fortune coins gain +11% Heads for the run (max +55%).";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.47;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2), Effect.FortuneOdds(0.11) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Compost Tails landed", 12, 45, 135,
            "Tails adds 1 less quota.", "Its Fortune odds bonus grows by 2%.", "Heads scores +1 point.",
            MasterySides.Tails, MasterySides.None, MasterySides.Heads);

        public CompostCoin()
        {
            On.Game.Coins.Resolved += (ctx,e) => { if(e.Inst==ctx.Coin && e.Res.Result==Side.Tails) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Tails) foreach(var effect in res.Effects) { if(effect.Type==EffectType.Penalty && ctx.Mastery.Level>=1) effect.Amount=Math.Max(0,effect.Amount-1); if(effect.Type==EffectType.FortuneOdds && ctx.Mastery.Level>=2) effect.Amount+=.02; } if(res.Result==Side.Heads && ctx.Mastery.Level>=3) res.Effects.Add(Effect.Score(1)); };
        }
    }
}
