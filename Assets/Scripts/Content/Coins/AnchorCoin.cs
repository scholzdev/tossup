using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class AnchorCoin : CoinDef
    {
        public override string Id => "anchor";
        public override string Name => "Anchor";
        public override string Description => "Heads: 2 points, and the next time the combo would break it holds instead.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.ComboShield(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Combo shields granted by Anchor", 15, 50, 150,
            "Heads scores +1 point.", "Heads grants a second combo shield.", "Tails scores +2 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Tails);

        public AnchorCoin()
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
