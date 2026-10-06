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
        }
    }
}
