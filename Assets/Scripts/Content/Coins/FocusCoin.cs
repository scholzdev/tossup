using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FocusCoin : CoinDef
    {
        public override string Id => "focus";
        public override string Name => "Focus";
        public override string Description => "Heads: the next coin gets +35% Heads. Tails: 4 points.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override IReadOnlyList<BuffSpec> Buffs { get; } = new[]
        {
            new BuffSpec("focus_next_odds", OutcomeSide.Heads, BuffTarget.NextCoins(), Effect.NextOdds(.35)),
        };
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Coins affected by Focus", 15, 50, 150,
            "Tails scores +1 point.", "Its Heads odds buff grows to +40%.", "Its buff reaches the next two coins.",
            MasterySides.Tails, MasterySides.None, MasterySides.None);

        public FocusCoin()
        {
            On.Game.Buffs.Applied += (ctx,e) => { if(e.Buff?.SourceUid==ctx.Coin.Uid && e.Buff.SpecId=="focus_next_odds") ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result==Side.Tails && ctx.Mastery.Level>=1) res.Effects.Add(Effect.Score(1)); };
            On.Coins.BuffCreated += (ctx,buff) => { if(buff.SpecId!="focus_next_odds") return; if(ctx.Mastery.Level>=2) buff.Amount=.40; if(ctx.Mastery.Level>=3) buff.Left=2; };
        }
    }
}
