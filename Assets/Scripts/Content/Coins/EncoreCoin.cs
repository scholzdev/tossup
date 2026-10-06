using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class EncoreCoin : CoinDef
    {
        public override string Id => "encore";
        public override string Name => "Encore";
        public override string Description => "If its first result this level is Tails, score 1 point and return it for one more flip.";
        public override string SpecialRule => "If its first result this level is Tails, it returns to the pile once.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 21;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public EncoreCoin()
        {
            On.Coins.Resolve += (context, result) =>
            {
                if (result.Result == Side.Tails && !context.Game.Encounter.BestScores.ContainsKey(context.Coin.Uid))
                    result.Effects.Add(Effect.ExtraDraw(1));
            };
        }
    }
}
