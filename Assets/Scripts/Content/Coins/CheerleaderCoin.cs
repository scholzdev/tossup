using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CheerleaderCoin : CoinDef
    {
        public override string Id => "cheerleader";
        public override string Name => "Cheerleader";
        public override string Description => "Heads: 2 points, next 2 coins +20% Heads. Tails: next coin +20%.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("cheerleader_heads_odds", OutcomeSide.Heads, BuffTarget.NextCoins(2), Effect.NextOdds(.2)),
            new BuffSpec("cheerleader_tails_odds", OutcomeSide.Tails, BuffTarget.NextCoins(), Effect.NextOdds(.2)),
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins affected by Cheerleader odds", 30, 120, 360,
            "Its odds buff improves by 2%.", "Its odds buff improves by 4% total.", "Its odds buff improves by 6% total.");

        public CheerleaderCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && (e.Buff.SpecId == "cheerleader_heads_odds" || e.Buff.SpecId == "cheerleader_tails_odds")) ctx.Mastery.Add(1); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "cheerleader_heads_odds" || buff.SpecId == "cheerleader_tails_odds") buff.Amount += .02 * ctx.Mastery.Level; };
        }
    }
}
