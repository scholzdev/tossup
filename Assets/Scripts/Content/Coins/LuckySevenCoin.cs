using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LuckySevenCoin : CoinDef
    {
        public override string Id => "lucky_seven";
        public override string Name => "Lucky Seven";
        public override string Description => "1 in 7: lands Heads and pays triple points.";
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
        {
            if (!inst.Jackpot) return;
            inst.Jackpot = false;
            foreach (var effect in res.Effects) effect.Amount *= 3;
        }

        public override void OnFlip(GameState game, CoinInst inst, FlipState flip)
        {
            inst.Jackpot = Rng.Int(game, 1, 7) == 7;
            if (inst.Jackpot) flip.Result = Side.Heads;
        }
    }
}
