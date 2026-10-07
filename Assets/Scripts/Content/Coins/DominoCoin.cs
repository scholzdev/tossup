using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DominoCoin : CoinDef
    {
        public override string Id => "domino";
        public override string Name => "Domino";
        public override string Description => "Heads: 2 points, and the next coin lands Heads. Tails: quota +1.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("domino_force_heads", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.NextHeads()),
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins given guaranteed Heads by Domino", 15, 60, 180,
            "Domino Heads scores +1.", "Domino Heads scores +2 total.", "Its Heads buff reaches a second coin.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.None);

        public DominoCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "domino_force_heads") ctx.Mastery.Add(1); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "domino_force_heads" && ctx.Mastery.Level >= 3) buff.Left = 2; };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(Math.Min(2, ctx.Mastery.Level))); };
        }
    }
}
