using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class HourglassCoin : CoinDef
    {
        public override string Id => "hourglass";
        public override string Name => "Hourglass";
        public override string Description => "+30% Heads when 3 or fewer coins are left. Tails: goes back into the pile.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.ExtraDraw(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnOdds(GameState game, CoinInst inst, Odds odds)
        {
            var level = game.Encounter;
            if (level != null && level.Queue.Count + level.Pile.Count <= 3) odds.Heads += .3;
        }
    }
}
