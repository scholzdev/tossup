using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ChainCoin : CoinDef
    {
        public override string Id => "chain";
        public override string Name => "Chain";
        public override string Description => "Heads: 2 points per Heads in a row, including this one.";
        public override string HeadsDescription => "Score 2 points per Heads in a row, including this one.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * 4;

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            res.Effects.Add(Effect.Score( 2 * game.Encounter.Streak));
        }
    }
}
