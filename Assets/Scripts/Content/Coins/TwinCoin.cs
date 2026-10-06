using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class TwinCoin : CoinDef
    {
        public override string Id => "twin";
        public override string Name => "Twin";
        public override string Description => "Always lands the same as the previous flip.";
        public override string SpecialRule => "Always lands the same as the previous flip.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnFlip(GameState game, CoinInst inst, FlipState flip)
        {
            var previous = game.LastResult;
            if (previous != null) flip.Result = previous.Final;
        }
    }
}
