using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SnowballCoin : CoinDef
    {
        public override string Id => "snowball";
        public override string Name => "Snowball";
        public override string Description => "Heads gains +1 point every flip for the whole run (max +8, or +10 with mastery).";
        public override string HeadsDescription => "Score 3 points, plus 1 for every stored Snowball stack.";
        public override string SpecialRule => "Gain 1 point of Heads power after each flip (max +8, or +10 at mastery level 2).";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 24;
        public override int EnergyCost => 1;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Snowball flips at maximum stacks", 15, 50, 150,
            "Heads scores +1 point.", "Its stack cap rises to 10.",
            "Tails scores +1 point.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Tails);

        public SnowballCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) =>
            { if (e.Inst == ctx.Coin && ctx.Coin.Stack >= (ctx.Mastery.Level >= 2 ? 10 : 8)) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result == Side.Heads && ctx.Mastery.Level >= 1) res.Effects.Add(Effect.Score(1));
                if (res.Result == Side.Tails && ctx.Mastery.Level >= 3) res.Effects.Add(Effect.Score(1));
            };
            On.Coins.Grow += (ctx, evt) =>
            {
                if (evt == CoinGrowthEvent.Flip)
                    ctx.Coin.Stack = Math.Min(ctx.Mastery.Level >= 2 ? 10 : 8, ctx.Coin.Stack + 1);
            };
        }

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * inst.Stack;

        public override void OnResolve(GameState g, CoinInst inst, Res res)
        {if(res.Result==Side.Heads)foreach(var effect in res.Effects)if(effect.Type==EffectType.Score)effect.Amount+=inst.Stack;}

    }
}
