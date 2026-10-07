using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class WindfallCoin : CoinDef
    {
        public override string Id => "windfall";
        public override string Name => "Windfall";
        public override string Description => "Heads: 6 gold. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 17;
        public override int EnergyCost => 0;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(6) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Gold earned from Windfall Heads", 60, 240, 720,
            "Heads grants +1 gold.", "Heads grants +2 gold total.", "Heads grants +3 gold total.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public WindfallCoin()
        {
            On.Game.Effects.Applied += (ctx, e) => { if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.Gold && e.GoldDelta > 0) ctx.Mastery.Add(e.GoldDelta); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Gold(ctx.Mastery.Level)); };
        }
    }
}
