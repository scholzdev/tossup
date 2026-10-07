using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class PremonitionCoin : CoinDef
    {
        public override string Id => "premonition";
        public override string Name => "Premonition";
        public override string Description => "Heads: the next coin lands Heads. Tails: 3 points. Edge: gain 1 energy.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.55;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[] { new BuffSpec("premonition_heads", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.NextHeads()) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins given guaranteed Heads by Premonition", 20, 80, 240,
            "Premonition Heads scores 1.", "Premonition Heads scores 2 total.", "Its forced-Heads buff reaches a second coin.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.None);

        public PremonitionCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "premonition_heads") ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(Math.Min(2, ctx.Mastery.Level))); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "premonition_heads" && ctx.Mastery.Level >= 3) buff.Left = 2; };
        }
    }
}
