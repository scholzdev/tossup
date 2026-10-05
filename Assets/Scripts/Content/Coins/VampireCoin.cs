using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class VampireCoin : CoinDef
    {
        public override string Id => "vampire";
        public override string Name => "Vampire";
        public override string Description => "Heads: 2 points and it drains 2 gold from the house.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        { if (res.Result == Side.Heads) res.Effects.Add(Effect.Gold( 2)); }
    }
}
