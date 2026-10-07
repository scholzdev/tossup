using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BloodPactCoin : CoinDef
    {
        public override string Id => "blood_pact";
        public override string Name => "Blood Pact";
        public override string Description => "Heads: next Blood coin gains 8 points on Heads or gives the enemy 4 on Tails. Edge gets half of both.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 32;
        public override int EnergyCost => 0;
        public override double Probability => 0.58;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("blood_pact_blood", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Blood), Effect.TypeBuff(kind: CoinType.Blood)),
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Blood coins affected by Blood Pact", 15, 60, 180,
            "Its Blood buff reaches a second coin.", "Blood Pact Heads scores +1.", "Its Blood buff reaches a third coin.",
            MasterySides.None, MasterySides.Heads, MasterySides.None);

        public BloodPactCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "blood_pact_blood") ctx.Mastery.Add(1); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "blood_pact_blood") buff.Left = 1 + (ctx.Mastery.Level >= 1 ? 1 : 0) + (ctx.Mastery.Level >= 3 ? 1 : 0); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Score(1)); };
        }
    }
}
