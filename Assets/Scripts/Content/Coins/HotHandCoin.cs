using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HotHandCoin : CoinDef
    {
        public override string Id => "hot_hand";
        public override string Name => "Hot Hand";
        public override string Description => "Heads: 2 points, and the combo grows by 1 extra step.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.ComboBonus(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Extra combo steps gained from Hot Hand", 20, 80, 240,
            "Heads scores +1.", "Heads scores +2 total.", "Heads adds another combo step.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public HotHandCoin()
        {
            On.Game.Effects.Applied += (ctx, e) => { if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.ComboBonus) ctx.Mastery.Add(e.Effect.Amount); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result != Side.Heads) return; if (ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(Math.Min(2, ctx.Mastery.Level))); if (ctx.Mastery.Level >= 3) res.Effects.Add(Effect.ComboBonus(1)); };
        }
    }
}
