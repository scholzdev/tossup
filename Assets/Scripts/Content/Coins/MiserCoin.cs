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
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Miser Heads with at least 30 gold", 12, 45, 135,
            "Heads scores per 9 gold instead of 10.", "Tails grants +1 gold.",
            "Heads scores +1 per 20 gold held.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public MiserCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) =>
            { if (e.Inst == ctx.Coin && e.Res.Result == Side.Heads && ctx.Game.Player.Gold >= 30) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result == Side.Tails && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Gold(1));
                if (res.Result != Side.Heads) return;
                double gold = ctx.Game.Player.Gold;
                if (ctx.Mastery.Level >= 1)
                    res.Effects.Add(Effect.Score(Math.Floor(gold / 9) - Math.Floor(gold / 10)));
                if (ctx.Mastery.Level >= 3) res.Effects.Add(Effect.Score(Math.Floor(gold / 20)));
            };
        }

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails) => heads * Math.Floor(game.Player.Gold / 10);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            double amount = Math.Floor(game.Player.Gold / 10);
            if (amount > 0) res.Effects.Add(Effect.Score( amount));
        }
    }
}
