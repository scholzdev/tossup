using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CaliperCoin : CoinDef
    {
        public override string Id => "caliper";
        public override string Name => "Caliper";
        public override string Description => "Heads: 2 points. At 1 energy or less, gain +12% Heads chance.";
        public override string SpecialRule => "While you have 1 energy or less, this coin gains +12% Heads chance.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Caliper Heads at 1 energy or less", 10, 35, 100,
            "Those Heads score +1 point.", "Those Heads grant 1 energy.", "Low-energy Heads chance rises to +18%.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public CaliperCoin()
        {
            On.Coins.Odds += (context, odds) =>
            {
                if (context.Game.Player.Energy <= 1) odds.Heads += context.Mastery.Level >= 3 ? 0.18 : 0.12;
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads || ctx.Game.Player.Energy > 1) return;
                ctx.Mastery.Add(1);
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(1));
                if (level >= 2) res.Effects.Add(Effect.Energy(1));
            };
        }
    }
}
