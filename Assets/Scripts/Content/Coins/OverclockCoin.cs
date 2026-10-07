using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class OverclockCoin : CoinDef
    {
        public override string Id => "overclock";
        public override string Name => "Overclock";
        public override string Description => "If its first result this level is Heads, score 3 points and return it to the pile for one extra flip.";
        public override string SpecialRule => "If its first result this level is Heads, it returns to the pile for one extra flip.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.55;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos, CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public OverclockCoin()
        {
            On.Coins.Resolve += (context, result) =>
            {
                if (result.Result == Side.Heads && !context.Game.Encounter.BestScores.ContainsKey(context.Coin.Uid))
                    result.Effects.Add(Effect.ExtraDraw(1));
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads || ctx.Game.Encounter.BestScores.ContainsKey(ctx.Coin.Uid) ||
                    ctx.Game.Encounter.Returned >= Game.ReturnCap) return;
                if (ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(Math.Min(2, ctx.Mastery.Level)));
                if (ctx.Mastery.Level >= 3) res.Effects.Add(Effect.Energy(1));
            };
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.ExtraDraw && e.ReturnedDelta > 0)
                    ctx.Mastery.Add(e.ReturnedDelta);
            };
        }

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "First Heads that return Overclock to the pile", 15, 60, 180,
            "That Heads scores +1.", "That Heads scores +2 total.", "That Heads also grants 1 energy.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);
    }
}
