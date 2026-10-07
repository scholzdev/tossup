using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HourglassCoin : CoinDef
    {
        public override string Id => "hourglass";
        public override string Name => "Hourglass Coin";
        public override string Description => "Every 2 seconds, its odds shift to favor Heads, Edge, then Tails";
        public override string SpecialRule => "Every 2 seconds, its odds shift to favor Heads, Edge, then Tails.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.4;
        public override double TieProbability => 0.15;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(7) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Energy(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Penalty(4) };

        public HourglassCoin()
        {
            On.Coins.Odds += (ctx, odds) =>
            {
                if (ctx.Game.Encounter == null) return;
                switch ((int)(ctx.ElapsedSeconds / 2) % 3)
                {
                    case 0: odds.Heads += 0.12; break;
                    case 1: odds.Edge += 0.12; break;
                    case 2: odds.Tails += 0.12; break;
                }
            };
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Flip.Final == Side.Heads && (int)(e.Flip.OddsSampleTime / 2) % 3 == 0) ctx.Mastery.Add(1); };
            On.Coins.Odds += (ctx, odds) => { if (ctx.Game.Encounter != null && (int)(ctx.ElapsedSeconds / 2) % 3 == 0) odds.Heads += .02 * ctx.Mastery.Level; };
        }

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Heads landed during the Heads-favored phase", 20, 80, 240,
            "Heads-favored odds gain another 2%.", "Heads-favored odds gain another 4% total.", "Heads-favored odds gain another 6% total.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);
    }
}
