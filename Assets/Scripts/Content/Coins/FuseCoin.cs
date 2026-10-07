using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FuseCoin : CoinDef
    {
        public override string Id => "fuse";
        public override string Name => "Fuse";
        public override string Description => "Discard it to charge +6. Heads spends all charge as points.";
        public override string SpecialRule => "Discard this coin to gain 6 charge.";
        public override string HeadsDescription => "Score 1 point plus all stored charge.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Charge spent on Fuse Heads", 30, 100, 300,
            "Discarding Fuse stores 7 charge instead of 6.",
            "Spending at least 10 charge also grants 1 energy.",
            "Fuse retains 20% of spent charge.",
            MasterySides.None, MasterySides.Heads, MasterySides.Heads);

        public FuseCoin()
        {
            On.Coins.Discard += ctx => ctx.Coin.Charge += ctx.Mastery.Level >= 1 ? 7 : 6;
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads || ctx.Coin.Charge <= 0) return;
                double spent = ctx.Coin.Charge;
                int level = ctx.Mastery.Level;
                res.Effects.Add(Effect.Score(spent));
                if (level >= 2 && spent >= 10) res.Effects.Add(Effect.Energy(1));
                ctx.Coin.Charge = level >= 3 ? Math.Floor(spent * .2) : 0;
                ctx.Mastery.Add(spent);
            };
        }
    }
}
