using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DividendCoin : CoinDef
    {
        public override string Id => "dividend";
        public override string Name => "Dividend";
        public override string Description => "Heads: gain 1 gold per 30 gold you hold, up to 3. Tails: score 1 point.";
        public override string HeadsDescription => "Gain 1 gold per 30 gold you hold, up to 3.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 21;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Gold earned from Dividend", 30, 120, 360,
            "Heads payout cap rises to 4 gold.", "Tails gains 1 gold.", "Heads payout cap rises to 5 gold.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public DividendCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.Gold) ctx.Mastery.Add(Math.Max(0, e.GoldDelta));
            };
            On.Coins.Resolve += (context, result) =>
            {
                if (result.Result == Side.Heads)
                {
                    int cap = context.Mastery.Level >= 3 ? 5 : context.Mastery.Level >= 1 ? 4 : 3;
                    result.Effects.Add(Effect.Gold(Math.Min(cap, 1 + Math.Floor(context.Game.Player.Gold / 30))));
                }
                else if (result.Result == Side.Tails && context.Mastery.Level >= 2)
                    result.Effects.Add(Effect.Gold(1));
            };
        }
    }
}
