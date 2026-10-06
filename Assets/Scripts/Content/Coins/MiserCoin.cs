using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MiserCoin : CoinDef
    {
        public override string Id => "miser";
        public override string Name => "Miser";
        public override string Description => "Heads: 1 point per 10 gold you hold.";
        public override string HeadsDescription => "Score 1 point per 10 gold you hold.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Gold(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * Math.Floor(game.Player.Gold / 10);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            double amount = Math.Floor(game.Player.Gold / 10);
            if (amount > 0) res.Effects.Add(Effect.Score( amount));
        }
    }
}
