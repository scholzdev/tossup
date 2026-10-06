using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SyncopationCoin : CoinDef
    {
        public override string Id => "syncopation";
        public override string Name => "Syncopation";
        public override string Description => "Heads: 2 points. Tails: 1 point. Repeating the previous side adds 1 combo step.";
        public override string SpecialRule => "Repeating the previous Heads or Tails result adds 1 combo step.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 20;
        public override int EnergyCost => 1;
        public override double Probability => 0.55;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public SyncopationCoin()
        {
            On.Coins.Resolve += (context, result) =>
            {
                var previous = context.Game.LastResult;
                if (previous != null && result.Result != Side.Tie && previous.Final == result.Result)
                    result.Effects.Add(Effect.ComboBonus(1));
            };
        }
    }
}
