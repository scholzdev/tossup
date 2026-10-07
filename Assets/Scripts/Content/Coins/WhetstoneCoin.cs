using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class WhetstoneCoin : CoinDef
    {
        public override string Id => "whetstone";
        public override string Name => "Whetstone";
        public override string Description => "Heads: next 2 Steel coins gain 3 points on Heads, but add 2 quota on Tails.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 22;
        public override int EnergyCost => 0;
        public override double Probability => 0.67;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("whetstone_steel", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Steel, 2), Effect.TypeBuff(kind: CoinType.Steel)),
        };

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Steel coins affected by Whetstone", 10, 40, 120,
            "Its buff grants +4 points on Heads.",
            "Buffed Tails adds 1 less quota.",
            "Its buff reaches a third Steel coin.");

        public WhetstoneCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) =>
            {
                if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "whetstone_steel")
                    ctx.Mastery.Add(1);
            };
            On.Coins.BuffCreated += (ctx, buff) =>
            {
                if (buff.SpecId != "whetstone_steel") return;
                int level = ctx.Mastery.Level;
                if (level >= 1) buff.Amount = 4;
                if (level >= 2) buff.PenaltyAmount = 1;
                if (level >= 3) buff.Left = 3;
            };
        }
    }
}
