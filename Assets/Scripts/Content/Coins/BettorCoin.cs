using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BettorCoin : CoinDef
    {
        public override string Id => "bettor";
        public override string Name => "Bettor";
        public override string Description => "Heads: 3 points per flip in the current combo (max 30). Tails: quota +2.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Quota(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * 6;

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            res.Effects.Add(Effect.Score( Math.Min(30, 3 * game.Encounter.ComboLen)));
        }
    }
}
