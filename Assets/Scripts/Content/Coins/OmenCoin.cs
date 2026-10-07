using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class OmenCoin : CoinDef
    {
        public override string Id => "omen";
        public override string Name => "Omen";
        public override string Description => "Heads: 3 points and the next coin gains 10% Heads chance. Tails: 2 points.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0.15;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[] { new BuffSpec("omen_odds", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.NextOdds(.1)) };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins influenced by Omen odds", 20, 80, 240,
            "Its odds buff grants +12% Heads.", "Its odds buff grants +14% Heads.", "Its odds buff grants +16% Heads.");

        public OmenCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "omen_odds") ctx.Mastery.Add(1); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "omen_odds") buff.Amount += .02 * ctx.Mastery.Level; };
        }
    }
}
