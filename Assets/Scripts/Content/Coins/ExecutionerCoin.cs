using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ExecutionerCoin : CoinDef
    {
        public override string Id => "executioner";
        public override string Name => "Executioner";
        public override string Description => "Heads: 9 points. Tails: add 2 to the quota.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 23;
        public override int EnergyCost => 1;
        public override double Probability => 0.38;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(9) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Executioner Heads landed", 10, 35, 100,
            "Heads scores +1 point.", "Tails adds 1 less quota.", "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public ExecutionerCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Res?.Result == Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Score(1));
                    if (level >= 3) res.Effects.Add(Effect.Score(2));
                }
                else if (res.Result == Side.Tails && level >= 2)
                    foreach (var effect in res.Effects) if (effect.Type == EffectType.Penalty) effect.Amount = Math.Max(0, effect.Amount - 1);
            };
        }
    }
}
