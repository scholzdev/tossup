using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class NormalCoin : CoinDef
    {
        public override string Id => "normal";
        public override string Name => "Normal";
        public override string Description => "A plain coin. Barely a scratch.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 5;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Points scored with Normal", 20, 100, 300,
            "Heads scores +1 point.",
            "Tails scores 1 point.",
            "The first Heads each level scores +3 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public NormalCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin) ctx.Mastery.Add(Math.Max(0, e.Flip?.Gained ?? 0));
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Score(1));
                    if (level >= 3 && ctx.Coin.MasteryFirstHeadsLevel != ctx.Game.EncounterIndex)
                    {
                        ctx.Coin.MasteryFirstHeadsLevel = ctx.Game.EncounterIndex;
                        res.Effects.Add(Effect.Score(3));
                    }
                }
                else if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Score(1));
            };
        }
    }
}
