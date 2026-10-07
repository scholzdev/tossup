using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BountyCoin : CoinDef
    {
        public override string Id => "bounty";
        public override string Name => "Bounty";
        public override string Description => "Pays 1 gold for every 2 points it scores.";
        public override string SpecialRule => "Pays 1 gold for every 2 points this coin scores.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Gold earned from Bounty scoring", 40, 160, 480,
            "Scoring effects grant +1 extra gold.", "Scoring effects grant +2 extra gold total.", "Scoring effects grant +3 extra gold total.",
            MasterySides.All, MasterySides.All, MasterySides.All);

        public BountyCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst != ctx.Coin || e.Effect?.Type != EffectType.Score) return;
                double baseGold = Math.Floor(e.ScoreDelta / 2);
                double paid = baseGold + ctx.Mastery.Level;
                if (paid > 0)
                {
                    ctx.Game.Player.Gold += paid;
                    e.GoldDelta += paid;
                    e.Text += ", +" + GameText.Num(paid) + " bounty gold";
                }
                ctx.Mastery.Add(baseGold);
            };
        }
    }
}
