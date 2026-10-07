using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class GamblerCoin : CoinDef
    {
        public override string Id => "gambler";
        public override string Name => "Gambler";
        public override string Description => "Heads is a bet: 50% triple points, otherwise nothing.";
        public override string HeadsDescription => "A 50% bet: triple the points or score nothing.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 22;
        public override int EnergyCost => 1;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(7) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Gambler bets won", 10, 35, 100,
            "Winning bets score +3 points.", "Heads chance +3%.",
            "Winning bets score another +3 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public GamblerCoin()
        {
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads || res.Effects.Count == 0) return;
                ctx.Mastery.Add(1);
                if (ctx.Mastery.Level >= 1)
                    res.Effects.Add(Effect.Score(ctx.Mastery.Level >= 3 ? 6 : 3));
            };
            On.Coins.Odds += (ctx, odds) =>
            { if (ctx.Mastery.Level >= 2) odds.Heads += .03; };
        }

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * 3.5;

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            if (Rng.Random(game) < .5)
            {
                foreach (var effect in res.Effects) effect.Amount *= 3;
                Game.Log(game, "Gambler wins the bet.");
            }
            else
            {
                res.Effects = new List<Effect>();
                Game.Log(game, "Gambler loses the bet.");
            }
        }
    }
}
