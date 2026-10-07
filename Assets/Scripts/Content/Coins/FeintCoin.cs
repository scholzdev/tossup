using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FeintCoin : CoinDef
    {
        public override string Id => "feint";
        public override string Name => "Feint";
        public override string Description => "Heads: 1 point and the next coin uses its other side. Tails: 3 points.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[] { new BuffSpec("feint_swap", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.NextSwap()) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins affected by Feint swaps", 20, 80, 240,
            "Feint Heads scores +1.", "Feint Heads scores +2 total.", "Its swap reaches a second coin.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.None);

        public FeintCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "feint_swap") ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(Math.Min(2, ctx.Mastery.Level))); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "feint_swap" && ctx.Mastery.Level >= 3) buff.Left = 2; };
        }
    }
}
