using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ConductorCoin : CoinDef
    {
        public override string Id => "conductor";
        public override string Name => "Conductor";
        public override string Description => "Heads: next 3 Rhythm coins gain 2 points per combo step (max 8); a broken combo gives the enemy 5.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 24;
        public override int EnergyCost => 0;
        public override double Probability => 0.73;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("conductor_rhythm", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Rhythm, 3), Effect.TypeBuff(kind: CoinType.Rhythm)),
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Rhythm coins affected by Conductor", 10, 40, 120,
            "Its buff reaches a fourth Rhythm coin.", "Conductor Heads scores 2 points.", "Its buff reaches a fifth Rhythm coin.",
            MasterySides.None, MasterySides.Heads, MasterySides.None);

        public ConductorCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) =>
            {
                if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "conductor_rhythm") ctx.Mastery.Add(1);
            };
            On.Coins.BuffCreated += (ctx, buff) =>
            {
                if (buff.SpecId != "conductor_rhythm") return;
                if (ctx.Mastery.Level >= 3) buff.Left = 5;
                else if (ctx.Mastery.Level >= 1) buff.Left = 4;
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result == Side.Heads && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
