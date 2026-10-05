using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CapacitorCoin : CoinDef
    {
        public override string Id => "capacitor";
        public override string Name => "Flux Capacitor";
        public override string Description => "Heads: 2 points per energy you hold. Tails: +1 energy.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Energy(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * 2 * Math.Max(0, game.Player.MaxEnergy - 1);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            res.Effects.Add(Effect.Score( 2 * game.Player.Energy));
        }
    }
}
