using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class OrchestraCoin : CoinDef
    {
        public override string Id => "orchestra";
        public override string Name => "Orchestra";
        public override string Description => "Heads: 2 points per different coin type in your deck.";
        public override string HeadsDescription => "Score 2 points per different coin type in your deck.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Gold(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails)
        {
            var definitions = new HashSet<CoinDef>();
            foreach (var coin in game.Coins) definitions.Add(coin.Definition);
            return heads * 2 * definitions.Count;
        }

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            var ids = new HashSet<string>(); foreach (var coin in game.Coins) ids.Add(coin.Id);
            res.Effects.Add(Effect.Score( 2 * ids.Count));
        }
    }
}
