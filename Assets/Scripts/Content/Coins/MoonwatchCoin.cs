using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MoonwatchCoin : CoinDef
    {
        public override string Id => "moonwatch";
        public override string Name => "Moonwatch";
        public override string Description => "Every 2 seconds, its odds shift between Heads and Edge.";
        public override string SpecialRule => "Every 2 seconds, the favorable side shifts between Heads and Edge.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos, CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Energy(1) };

        public MoonwatchCoin()
        {
            On.Coins.Odds += (ctx, odds) =>
            {
                if ((int)(ctx.ElapsedSeconds / 2) % 2 == 0) odds.Heads += 0.12;
                else odds.Edge += 0.12;
            };
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Flip.Final == Side.Tie) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Tie && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level)); };
        }

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Edge results landed with Moonwatch", 20, 80, 240,
            "Edge also scores 1 point.", "Edge also scores 2 points total.", "Edge also scores 3 points total.",
            MasterySides.Edge, MasterySides.Edge, MasterySides.Edge);
    }
}
