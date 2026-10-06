using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SnowballCoin : CoinDef
    {
        public override string Id => "snowball";
        public override string Name => "Snowball";
        public override string Description => "Heads gains +1 point every flip for the whole run (max +8).";
        public override string HeadsDescription => "Score 3 points, plus 1 for every stored Snowball stack.";
        public override string SpecialRule => "Gain 1 permanent point of Heads power after each flip (max +8).";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 24;
        public override int EnergyCost => 1;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * inst.Stack;

        public override void OnResolve(GameState g, CoinInst inst, Res res)
        {if(res.Result==Side.Heads)foreach(var effect in res.Effects)if(effect.Type==EffectType.Score)effect.Amount+=inst.Stack;}

        public override void Grow(CoinInst inst, CoinGrowthEvent evt)
        {if(evt==CoinGrowthEvent.Flip)inst.Stack=Math.Min(8,inst.Stack+1);}
    }
}
