using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HoroscopeCoin : CoinDef
    {
        public override string Id => "horoscope";
        public override string Name => "Horoscope";
        public override string Description => "Heads: 1 point, all coins +7% Heads this level. Tails: all coins +3%.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.AllOdds(0.07) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.AllOdds(0.03) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Horoscope odds bonuses granted", 15, 50, 150,
            "Heads scores +1 point.", "Tails scores 1 point.", "Both sides grant +2% more Heads odds.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.None);

        public HoroscopeCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.AllOdds) ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads && level >= 1) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Score(1));
                if (level >= 3 && (res.Result == Side.Heads || res.Result == Side.Tails))
                    res.Effects.Add(Effect.AllOdds(0.02));
            };
        }
    }
}
