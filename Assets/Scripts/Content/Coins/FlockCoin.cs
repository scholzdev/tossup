using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FlockCoin : CoinDef
    {
        public override string Id => "flock";
        public override string Name => "Flock";
        public override string Description => "+10% Heads for every other Flock in your deck.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnOdds(GameState game, CoinInst inst, Odds odds)
        {
            foreach (var other in game.Coins)
                if (other != inst && other.Id == inst.Id) odds.Heads += .1;
        }
    }
}
