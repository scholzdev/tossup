using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CursedCoin : CoinDef
    {
        public override string Id => "cursed";
        public override string Name => "Cursed";
        public override string Description => "A powerful, dangerous wager.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(15) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Cursed Tails endured", 10, 35, 100,
            "Tails adds 1 less quota.", "Heads scores +2 points.", "Heads chance +3%.",
            MasterySides.Tails, MasterySides.Heads, MasterySides.Heads);

        public CursedCoin()
        {
            On.Game.Coins.Resolved += (ctx,e) => { if(e.Inst==ctx.Coin && e.Res.Result==Side.Tails) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Tails && ctx.Mastery.Level>=1) foreach(var effect in res.Effects) if(effect.Type==EffectType.Penalty) effect.Amount=Math.Max(0,effect.Amount-1); if(res.Result==Side.Heads && ctx.Mastery.Level>=2) res.Effects.Add(Effect.Score(2)); };
            On.Coins.Odds += (ctx,odds) => { if(ctx.Mastery.Level>=3) odds.Heads+=.03; };
        }
    }
}
