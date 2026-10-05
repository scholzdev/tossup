using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CashOutCoin : CoinDef
    {
        public override string Id => "cash_out";
        public override string Name => "Cash Out";
        public override string Description => "Heads: 3 points, squares the combo multiplier, banks its pot, then resets the combo.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        { if (res.Result == Side.Heads) res.CashOut = true; }
    }
}
