using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SafePort : CoinDef
    {
        public override string Id => "safeport";
        public override string Name => "Safe Port";
        public override string Description => string.Empty;
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 18;
        public override int EnergyCost => 1;
        public override double Probability => 1.0;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge { get; } = Array.Empty<Effect>();
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Safe Port Heads landed", 25, 100, 300,
            "Heads scores +1 point.", "Heads gains 1 gold.", "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public SafePort()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Res?.Result == Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads) return;
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(1));
                if (level >= 2) res.Effects.Add(Effect.Gold(1));
                if (level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
