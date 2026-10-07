using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    // Colorless glue coins: no CoinType, low variance, in every character's pool. One shared (Normal-style)
    // mastery; Gilded is never sold, it only comes from stamping a Normal (see Game.StampNormal).
    public class ColorlessCoin : CoinDef
    {
        public static readonly string[] Ids = { "slug", "spare_change", "steady_hand", "ballast", "clipped", "gilded" };

        readonly string id, name, desc;
        readonly double p, tie;
        readonly int cost;

        protected ColorlessCoin(string id, string name, string desc, double p, int cost, IReadOnlyList<Effect> tails, double tie = 0, IReadOnlyList<Effect> edge = null)
        {
            this.id = id; this.name = name; this.desc = desc; this.p = p; this.cost = cost; this.tie = tie;
            Tails = tails ?? Array.Empty<Effect>();
            Edge = edge ?? Effect.HalfOf(Tails, Heads);
            Mastery = new CoinMastery(
                "Points scored with " + name, 20, 100, 300,
                "Heads scores +1 point.",
                "Tails scores 1 point.",
                "The first Heads each level scores +3 points.",
                MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);
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

        public override string Id => id;
        public override string Name => name;
        public override string Description => desc;
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => cost;
        public override int EnergyCost => 0;
        public override double Probability => p;
        public override double TieProbability => tie;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; }
        public override IReadOnlyList<Effect> Edge { get; }
        public override CoinMastery Mastery { get; }
    }

    public sealed class SlugCoin : ColorlessCoin
    {
        public SlugCoin() : base("slug", "Slug", "Slow, steady, and almost always Heads.", .80, 6, new[] { Effect.Gold(1) }) { }
    }

    public sealed class SpareChangeCoin : ColorlessCoin
    {
        public SpareChangeCoin() : base("spare_change", "Spare Change", "Found it in the couch.", .50, 6, new[] { Effect.Gold(2) }) { }
    }

    public sealed class SteadyHandCoin : ColorlessCoin
    {
        public SteadyHandCoin() : base("steady_hand", "Steady Hand", "A miss just steadies the next toss.", .60, 6, null) { }
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[] { new BuffSpec("steady_odds", OutcomeSide.Tails, BuffTarget.NextCoins(), Effect.NextOdds(.2)) };
    }

    public sealed class BallastCoin : ColorlessCoin
    {
        public BallastCoin() : base("ballast", "Ballast", "Keeps the streak from tipping over.", .65, 6, new[] { Effect.ComboShield(1) }) { }
    }

    public sealed class ClippedCoin : ColorlessCoin
    {
        public ClippedCoin() : base("clipped", "Clipped", "Shaved down; it likes to land on its rim.", .45, 5, Array.Empty<Effect>(), .20, new[] { Effect.Score(2) }) { }
    }

    public sealed class GildedCoin : ColorlessCoin
    {
        public GildedCoin() : base("gilded", "Gilded", "A Normal coin with a better finish.", .75, 7, Array.Empty<Effect>()) { }
    }
}
