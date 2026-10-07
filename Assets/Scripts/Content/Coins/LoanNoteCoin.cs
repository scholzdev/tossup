using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LoanNoteCoin : CoinDef
    {
        public override string Id => "loan_note";
        public override string Name => "Loan Note";
        public override string Description => "Heads: gain 7 gold. Tails: lose 1 gold and add 1 to the quota. Edge: gain 1 gold.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 14;
        public override int EnergyCost => 0;
        public override double Probability => 0.4;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(7) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(1), Effect.Quota(1) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Gold(1) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Gold repaid on Loan Note Tails", 10, 35, 100,
            "Heads gains +3 gold.", "Tails no longer adds quota.", "Heads gains another +4 gold.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public LoanNoteCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.GoldLoss) ctx.Mastery.Add(Math.Max(0, -e.GoldDelta));
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Gold(3));
                    if (level >= 3) res.Effects.Add(Effect.Gold(4));
                }
                else if (res.Result == Side.Tails && level >= 2)
                {
                    var ownPenalty = res.Effects.Find(effect => effect.Type == EffectType.Penalty && effect.Amount == 1);
                    if (ownPenalty != null) res.Effects.Remove(ownPenalty);
                }
            };
        }
    }
}
