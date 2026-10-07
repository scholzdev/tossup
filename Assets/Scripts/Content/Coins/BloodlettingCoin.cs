using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BloodlettingCoin : CoinDef
    {
        public override string Id => "bloodletting";
        public override string Name => "Bloodletting";
        public override string Description => "Heads: 5 points. Tails: lose 1 gold.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 19;
        public override int EnergyCost => 1;
        public override double Probability => 0.45;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(5) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Gold actually lost to Bloodletting Tails", 15, 60, 180,
            "Heads scores +1.", "Heads scores +2 total.", "Heads scores +3 total.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public BloodlettingCoin()
        {
            On.Game.Effects.Applied += (ctx, e) => { if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.GoldLoss && e.GoldDelta < 0) ctx.Mastery.Add(-e.GoldDelta); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level)); };
        }
    }
}
