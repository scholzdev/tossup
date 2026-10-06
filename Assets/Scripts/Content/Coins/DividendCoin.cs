using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DividendCoin : CoinDef
    {
        public override string Id => "dividend";
        public override string Name => "Dividend";
        public override string Description => "Heads: gain 1 gold per 30 gold you hold, up to 3. Tails: score 1 point.";
        public override string HeadsDescription => "Gain 1 gold per 30 gold you hold, up to 3.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 21;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public DividendCoin()
        {
            On.Coins.Resolve += (context, result) =>
            {
                if (result.Result == Side.Heads)
                    result.Effects.Add(Effect.Gold(Math.Min(3, 1 + Math.Floor(context.Game.Player.Gold / 30))));
            };
        }
    }
}
