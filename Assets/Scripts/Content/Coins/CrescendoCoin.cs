using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CrescendoCoin : CoinDef
    {
        public override string Id => "crescendo";
        public override string Name => "Crescendo";
        public override string Description => "Heads: 3 points and extend the combo by 1. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3), Effect.ComboBonus(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Extra combo steps earned with Crescendo", 20, 80, 240,
            "Heads scores +1.", "Heads scores +2 total.", "Heads grants a second extra combo step.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public CrescendoCoin()
        {
            On.Game.Effects.Applied += (ctx, e) => { if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.ComboBonus) ctx.Mastery.Add(e.Effect.Amount); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result != Side.Heads) return; if (ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(Math.Min(2, ctx.Mastery.Level))); if (ctx.Mastery.Level >= 3) res.Effects.Add(Effect.ComboBonus(1)); };
        }
    }
}
