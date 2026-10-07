using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BrokerCoin : CoinDef
    {
        public override string Id => "broker";
        public override string Name => "Broker";
        public override string Description => "Heads: 2 gold and the next Steel coin scores +2 on Heads. Tails: 3 points.";
        public override string HeadsDescription => "Gain 2 gold and give the next Steel coin +2 points on Heads.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 18;
        public override int EnergyCost => 1;
        public override double Probability => 0.55;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("broker_steel", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Steel), Effect.Score(2), OutcomeSide.Heads)
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Steel coins affected by Broker", 15, 60, 180,
            "Its Steel buff scores +3.", "Its Steel buff scores +4.", "Its Steel buff scores +5.");

        public BrokerCoin()
        {
            On.Game.Buffs.Applied += (ctx, e) => { if (e.Buff?.SourceUid == ctx.Coin.Uid && e.Buff.SpecId == "broker_steel") ctx.Mastery.Add(1); };
            On.Coins.BuffCreated += (ctx, buff) => { if (buff.SpecId == "broker_steel") buff.AppliedEffect.Amount += ctx.Mastery.Level; };
        }
    }
}
