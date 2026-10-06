using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class AllInCoin : CoinDef
    {
        public override string Id => "allin";
        public override string Name => "All-In Coin";
        public override string Description => "Heads doubles its score. Tails scores nothing.";
        public override string HeadsDescription => "Doubles the score.";
        public override string TailsDescription => "Scores nothing.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new [] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Edge { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();

        public AllInCoin()
        {
            On.Coins.Resolve += Resolve;
        }

        static void Resolve(HookContext context, Res res)
        {
            if (res.Result != Side.Heads)
            {
                res.Effects.Clear();
                return;
            }

            res.Effects.Add(Effect.Score(5));
            foreach (var effect in res.Effects)
                if (effect.Type == EffectType.Score) effect.Amount *= 2;
        }

    }
}
