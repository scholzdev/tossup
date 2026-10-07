using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SeedlingCoin : CoinDef
    {
        public override string Id => "seedling";
        public override string Name => "Seedling";
        public override string Description => "Heads: 2 points and permanently gain 3% Heads chance. This can happen once each level. Tails: 2 points.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.55;
        public override double TieProbability => 0.05;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.FortuneOdds(0.03) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Energy(1) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Levels where Seedling grows its Fortune odds", 15, 60, 180,
            "Its growth grants +3.5% Heads.", "Its growth grants +4% Heads.", "Its growth grants +4.5% Heads.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public SeedlingCoin()
        {
            On.Coins.Resolve += (ctx, res) =>
            {
                if (ctx.Coin.CompostLevel == ctx.Game.EncounterIndex) return;
                foreach (var effect in res.Effects)
                    if (effect.Type == EffectType.FortuneOdds && ctx.Game.FortuneBonus < .55)
                    {
                        ctx.Mastery.Add(1);
                        effect.Amount += .005 * ctx.Mastery.Level;
                    }
            };
        }
    }
}
