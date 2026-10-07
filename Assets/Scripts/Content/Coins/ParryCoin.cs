using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ParryCoin : CoinDef
    {
        public override string Id => "parry";
        public override string Name => "Parry";
        public override string Description => "Heads: 1 point and a combo shield. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 12;
        public override int EnergyCost => 0;
        public override double Probability => 0.55;
        public override double TieProbability => 0.05;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.ComboShield(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Parry combo shields granted", 15, 50, 150,
            "Heads scores +1 point.", "Heads grants a second combo shield.", "Tails scores +2 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Tails);

        public ParryCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.ComboShield) ctx.Mastery.Add(e.Effect.Amount);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Score(1));
                    if (level >= 2) res.Effects.Add(Effect.ComboShield(1));
                }
                else if (res.Result == Side.Tails && level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
