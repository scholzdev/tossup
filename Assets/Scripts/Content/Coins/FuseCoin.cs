using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FuseCoin : CoinDef
    {
        public override string Id => "fuse";
        public override string Name => "Fuse";
        public override string Description => "Discard it to charge +6. Heads spends all charge as points.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 10;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * inst.Charge;

        public override void OnDiscard(GameState game, CoinInst inst)
        { inst.Charge += 6; }

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads || inst.Charge == 0) return;
            res.Effects.Add(Effect.Score( inst.Charge));
            inst.Charge = 0;
        }
    }
}
