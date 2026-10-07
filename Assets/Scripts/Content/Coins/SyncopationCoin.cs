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
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Previous sides repeated by Syncopation", 10, 40, 120,
            "A repeat scores +1 point.", "A repeat adds a second combo step.", "A repeat scores another +2 points.",
            MasterySides.Heads | MasterySides.Tails, MasterySides.Heads | MasterySides.Tails, MasterySides.Heads | MasterySides.Tails);

        public SyncopationCoin()
        {
            On.Coins.Resolve += (context, result) =>
            {
                var previous = context.Game.LastResult;
                if (previous != null && result.Result != Side.Tie && previous.Final == result.Result)
                {
                    context.Mastery.Add(1);
                    result.Effects.Add(Effect.ComboBonus(1));
                    if (context.Mastery.Level >= 1) result.Effects.Add(Effect.Score(1));
                    if (context.Mastery.Level >= 2) result.Effects.Add(Effect.ComboBonus(1));
                    if (context.Mastery.Level >= 3) result.Effects.Add(Effect.Score(2));
                }
            };
        }
    }
}
