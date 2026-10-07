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
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Points won on All-In Heads", 100, 400, 1200,
            "Heads adds 1 point before doubling.", "Heads gains 1 gold.", "Heads adds another 2 points before doubling.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public AllInCoin()
        {
            On.Coins.Resolve += Resolve;
            On.Game.Coins.Resolved += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Res?.Result == Side.Heads)
                    ctx.Mastery.Add(Math.Max(0, e.Flip?.Gained ?? 0));
            };
        }

        static void Resolve(HookContext context, Res res)
        {
            if (res.Result != Side.Heads)
            {
                res.Effects.Clear();
                return;
            }

            res.Effects.Add(Effect.Score(5));
            int level = context.Mastery.Level;
            if (level >= 1) res.Effects.Add(Effect.Score(1));
            if (level >= 2) res.Effects.Add(Effect.Gold(1));
            if (level >= 3) res.Effects.Add(Effect.Score(2));
            foreach (var effect in res.Effects)
                if (effect.Type == EffectType.Score) effect.Amount *= 2;
        }

    }
}
