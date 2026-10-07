using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class JesterCoin : CoinDef
    {
        static readonly Effect[] Results = { Effect.Score(6), Effect.Gold(4), Effect.Energy(2), Effect.Score(2) };
        public override string Id => "jester";
        public override string Name => "Jester";
        public override string Description => "Heads or Tails, it does something random.";
        public override string HeadsDescription => "Trigger a random effect.";
        public override string TailsDescription => "Trigger a random effect.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            var pick = Results[Rng.Int(game, 1, Results.Length) - 1];
            res.Effects = new List<Effect> { new Effect(pick.Type, pick.Amount) };
        }
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Random Jester effects resolved", 25, 100, 300,
            "Every result also scores 1 point.", "Every result also scores 2 points total.", "Every result also scores 3 points total.",
            MasterySides.All, MasterySides.All, MasterySides.All);

        public JesterCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level)); };
        }
    }
}
