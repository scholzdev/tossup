using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class PotCoin : CoinDef
    {
        public override string Id => "pot";
        public override string Name => "Pot";
        public override string Description => "Heads: points equal to the flips made so far this level (max 8).";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * Math.Min(8, (game.Coins.Count + 1) / 2.0);

        public override void OnResolve(GameState g, CoinInst i, Res r)
        {if(r.Result==Side.Heads)r.Effects.Add(Effect.Score(Math.Min(8,g.Encounter.Flips)));}
    }
}
