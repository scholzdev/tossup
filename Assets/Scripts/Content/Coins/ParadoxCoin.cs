using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ParadoxCoin : CoinDef
    {
        public override string Id => "paradox";
        public override string Name => "Paradox";
        public override string Description => "Heads: 3 points. Tails: the next coin uses its other side. Edge: 2 points and the next coin uses its other side.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 26;
        public override int EnergyCost => 1;
        public override double Probability => 0.45;
        public override double TieProbability => 0.2;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[] { new BuffSpec("paradox_swap", OutcomeSide.Tails, BuffTarget.NextCoins(), Effect.NextSwap()), new BuffSpec("paradox_swap", OutcomeSide.Edge, BuffTarget.NextCoins(), Effect.NextSwap()) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins affected by Paradox swaps", 15, 60, 180,
            "Paradox Tails scores 1 point.", "Paradox Edge scores +1.", "Its swap reaches a second coin.",
            MasterySides.Tails, MasterySides.Edge, MasterySides.None);

        public ParadoxCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "paradox_swap") ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Tails && ctx.Mastery.Level >= 1) res.Effects.Add(Effect.Score(1)); if (res.Result == Side.Tie && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Score(1)); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "paradox_swap" && ctx.Mastery.Level >= 3) buff.Left = 2; };
        }
    }
}
