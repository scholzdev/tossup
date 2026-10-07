using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SparkCoin : CoinDef
    {
        public override string Id => "spark";
        public override string Name => "Spark";
        public override string Description => "Energy or a small strike.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 9;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Energy(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Spark energy generated", 30, 100, 300,
            "Tails scores +1 point.", "Heads generates +1 energy.", "Heads chance +8%.",
            MasterySides.Tails, MasterySides.Heads, MasterySides.Heads);

        public SparkCoin()
        {
            On.Game.Effects.Applied += (ctx,e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.Energy)
                    ctx.Mastery.Add(Math.Max(0, e.EnergyDelta));
            };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Tails && ctx.Mastery.Level>=1) res.Effects.Add(Effect.Score(1)); if(res.Result==Side.Heads && ctx.Mastery.Level>=2) res.Effects.Add(Effect.Energy(1)); };
            On.Coins.Odds += (ctx,odds) => { if(ctx.Mastery.Level>=3) odds.Heads+=.08; };
        }
    }
}
