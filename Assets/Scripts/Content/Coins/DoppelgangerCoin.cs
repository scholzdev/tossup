using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DoppelgangerCoin : CoinDef
    {
        public override string Id => "doppelganger";
        public override string Name => "Doppelganger";
        public override string Description => "Heads: next Chaos coin applies its resolved effects twice, including penalties.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 34;
        public override int EnergyCost => 0;
        public override double Probability => 0.53;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("doppelganger_chaos", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Chaos), Effect.TypeBuff(kind: CoinType.Chaos)),
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Chaos coins affected by Doppelganger", 15, 60, 180,
            "Doppelganger Heads scores +1.", "Its buff reaches a second Chaos coin.", "Doppelganger Heads scores +3 total.",
            MasterySides.Heads, MasterySides.None, MasterySides.Heads);

        public DoppelgangerCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "doppelganger_chaos") ctx.Mastery.Add(1); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "doppelganger_chaos" && ctx.Mastery.Level >= 2) buff.Left = 2; };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level >= 3 ? 3 : 1)); };
        }
    }
}
