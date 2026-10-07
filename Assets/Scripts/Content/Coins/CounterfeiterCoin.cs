using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CounterfeiterCoin : CoinDef
    {
        public override string Id => "counterfeiter";
        public override string Name => "Counterfeiter";
        public override string Description => "Heads: next 2 Greed coins double all gold gained. Each Tails also loses up to 3 gold.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 25;
        public override int EnergyCost => 0;
        public override double Probability => 0.61;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.GoldLoss(3) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("counterfeiter_greed", OutcomeSide.Heads, BuffTarget.NextOfType(CoinType.Greed, 2), Effect.TypeBuff(kind: CoinType.Greed)),
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Greed coins affected by Counterfeiter", 12, 45, 135,
            "Tails loses 1 less gold.", "Its buff reaches three Greed coins.", "Heads also scores 2 points.",
            MasterySides.Tails, MasterySides.None, MasterySides.Heads);

        public CounterfeiterCoin()
        {
            On.Game.Buffs.Applied += (ctx,e) => { if(e.Buff?.SourceUid==ctx.Coin.Uid && e.Buff.SpecId=="counterfeiter_greed") ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Tails && ctx.Mastery.Level>=1) foreach(var effect in res.Effects) if(effect.Type==EffectType.GoldLoss) effect.Amount=Math.Max(0,effect.Amount-1); if(res.Result==Side.Heads && ctx.Mastery.Level>=3) res.Effects.Add(Effect.Score(2)); };
            On.Coins.BuffCreated += (ctx,buff) => { if(buff.SpecId=="counterfeiter_greed" && ctx.Mastery.Level>=2) buff.Left=3; };
        }
    }
}
