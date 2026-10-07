using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MirrorCoin : CoinDef
    {
        public override string Id => "mirror";
        public override string Name => "Mirror";
        public override string Description => "Heads: the next coin uses the effects of its other side. Tails: 2 points.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("mirror_swap_next", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.NextSwap()),
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins affected by Mirror swaps", 15, 60, 180,
            "Mirror Heads scores +1.", "Mirror Tails scores +1.", "Mirror Heads scores +2 total.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public MirrorCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "mirror_swap_next") ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level >= 1) res.Effects.Add(Effect.Score(ctx.Mastery.Level >= 3 ? 2 : 1)); else if (res.Result == Side.Tails && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Score(1)); };
        }
    }
}
