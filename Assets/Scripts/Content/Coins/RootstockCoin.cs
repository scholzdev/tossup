using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class RootstockCoin : CoinDef
    {
        public override string Id => "rootstock";
        public override string Name => "Rootstock";
        public override string Description => "Heads: 2 points, plus 1 per level started (max +5). Tails: 1 energy.";
        public override string HeadsDescription => "Score 2 points, plus 1 per level started (max +5).";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 20;
        public override int EnergyCost => 0;
        public override double Probability => 0.45;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Edge => Array.Empty<Effect>();
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Rootstock copies grown at level start", 10, 35, 100,
            "Growth cap rises to +6 points.", "Tails grants another energy.", "Growth cap rises to +7 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public RootstockCoin()
        {
            On.Coins.Grow += (context, growth) =>
            {
                if (growth != CoinGrowthEvent.Level) return;
                context.Mastery.Add(1);
                int cap = context.Mastery.Level >= 3 ? 7 : context.Mastery.Level >= 1 ? 6 : 5;
                context.Coin.Stack = Math.Min(cap, context.Coin.Stack + 1);
            };
            On.Coins.Resolve += (context, result) =>
            {
                if (result.Result == Side.Heads) result.Effects.Add(Effect.Score(2 + context.Coin.Stack));
                if (result.Result == Side.Tails && context.Mastery.Level >= 2) result.Effects.Add(Effect.Energy(1));
            };
        }
    }
}
