using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SalvageCoin : CoinDef
    {
        public override string Id => "salvage";
        public override string Name => "Salvage";
        public override string Description => "Heads: 3 points. Tails: 1 point. Discarding it grants 2 energy.";
        public override string SpecialRule => "Discarding this coin grants 2 energy.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override void OnDiscard(GameState game, CoinInst inst) => game.Player.Energy += 2;
    }
}
