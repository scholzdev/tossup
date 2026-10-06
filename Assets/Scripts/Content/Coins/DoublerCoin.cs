using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DoublerCoin : CoinDef
    {
        public override string Id => "doubler";
        public override string Name => "Doubler";
        public override string Description => "Heads: 3 points, doubled for every Doubler flip so far this level (3, 6, 12, 24... up to 384).";
        public override string HeadsDescription => "Score 3 points, doubled for every Doubler flipped earlier this level (max 384).";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails)
        {
            int copies = 0;
            foreach (var coin in game.Coins) if (coin.Definition == this) copies++;
            return heads * 3 * Math.Pow(2, Math.Min(copies - 1, 3));
        }

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            var e = game.Encounter;
            int flips = e.Doubler;
            e.Doubler = flips + 1;
            if (res.Result == Side.Heads) res.Effects.Add(Effect.Score( 3 * Math.Pow(2, Math.Min(flips, 7))));
        }
    }
}
