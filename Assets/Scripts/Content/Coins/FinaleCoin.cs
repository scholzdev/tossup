using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FinaleCoin : CoinDef
    {
        public override string Id => "finale";
        public override string Name => "Finale";
        public override string Description => "Heads: 4 points and add 2 combo steps. Tails: gain 2 gold.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 25;
        public override int EnergyCost => 1;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4), Effect.ComboBonus(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Gold(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Combo steps added by Finale", 30, 100, 300,
            "Heads scores +1 point.", "Tails gains +1 gold.", "Heads adds another combo step.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public FinaleCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.ComboBonus) ctx.Mastery.Add(e.Effect.Amount);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Score(1));
                    if (level >= 3) res.Effects.Add(Effect.ComboBonus(1));
                }
                else if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Gold(1));
            };
        }
    }
}
