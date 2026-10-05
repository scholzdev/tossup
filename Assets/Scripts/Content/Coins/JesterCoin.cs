using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class JesterCoin : CoinDef
    {
        static readonly Effect[] Results = { Effect.Score(6), Effect.Gold(4), Effect.Energy(2), Effect.Score(2) };
        public override string Id => "jester";
        public override string Name => "Jester";
        public override string Description => "Heads or Tails, it does something random.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => 2;

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            var pick = Results[Rng.Int(game, 1, Results.Length) - 1];
            res.Effects = new List<Effect> { new Effect(pick.Type, pick.Amount) };
        }
    }
}
