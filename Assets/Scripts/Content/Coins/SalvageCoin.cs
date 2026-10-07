using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SalvageCoin : CoinDef
    {
        public override string Id => "salvage";
        public override string Name => "Salvage";
        public override string Description => "Heads: 3 points. Tails: 1 point. Discarding it grants 2 energy.";
        public override string SpecialRule => "Discarding this coin grants 2 energy.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Salvage discards", 8, 30, 90,
            "Discarding grants 3 energy instead of 2.", "Tails scores +1 point.", "Heads scores +2 points.",
            MasterySides.None, MasterySides.Tails, MasterySides.Heads);

        public SalvageCoin()
        {
            On.Coins.Discard += ctx =>
            {
                ctx.Game.Player.Energy += ctx.Mastery.Level >= 1 ? 3 : 2;
                ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result == Side.Tails && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Heads && ctx.Mastery.Level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
