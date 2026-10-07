using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class RepriseCoin : CoinDef
    {
        public override string Id => "reprise";
        public override string Name => "Reprise";
        public override string Description => "Heads: 3 points. Tails: 1 gold. Returns to the pile after its first play each level.";
        public override string SpecialRule => "The first time this coin resolves each level, it returns to the pile once.";
        public override string HeadsDescription => "Score 3 points.";
        public override string TailsDescription => "Gain 1 gold.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.55;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Gold(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "First Reprise plays across levels", 10, 35, 100,
            "Heads scores +1 point.", "Tails gains +3 gold.", "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public RepriseCoin()
        {
            On.Coins.Resolve += ReturnOnce;
            On.Coins.Resolve += (ctx, res) =>
            {
                if (!ctx.Game.Encounter.BestScores.ContainsKey(ctx.Coin.Uid)) ctx.Mastery.Add(1);
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Score(1));
                    if (level >= 3) res.Effects.Add(Effect.Score(2));
                }
                else if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Gold(3));
            };
        }

        static void ReturnOnce(HookContext context, Res res)
        {
            // BestScores is written after Resolve hooks run, so its absence means this is
            // the coin's first resolved flip this level, regardless of the side it landed on.
            if (!context.Game.Encounter.BestScores.ContainsKey(context.Coin.Uid))
                res.Effects.Add(Effect.ExtraDraw(1));
        }
    }
}
