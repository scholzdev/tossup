using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SunderCoin : CoinDef
    {
        public override string Id => "sunder";
        public override string Name => "Sunder";
        public override string Description => "Heads: 7 points. Tails: add 1 to the quota.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 21;
        public override int EnergyCost => 1;
        public override double Probability => 0.38;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(7) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Sunder Heads landed", 20, 80, 240,
            "Heads scores +1.", "Heads scores +2 total.", "Heads scores +3 total.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public SunderCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Flip.Final == Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level)); };
        }
    }
}
