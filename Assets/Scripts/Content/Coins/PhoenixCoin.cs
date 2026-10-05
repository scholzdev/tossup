using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class PhoenixCoin : CoinDef
    {
        public override string Id => "phoenix";
        public override string Name => "Phoenix";
        public override string Description => "Each Tails stores anger (max 5). Heads: 3 points +2 per anger.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result == Side.Tails) inst.Anger = Math.Min(5, inst.Anger + 1);
            else
            {
                foreach (var effect in res.Effects)
                    if (effect.Type == EffectType.Score) effect.Amount += 2 * inst.Anger;
                inst.Anger = 0;
            }
        }
    }
}
