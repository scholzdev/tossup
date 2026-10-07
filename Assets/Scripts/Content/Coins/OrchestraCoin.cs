using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class OrchestraCoin : CoinDef
    {
        public override string Id => "orchestra";
        public override string Name => "Orchestra";
        public override string Description => "Heads: 2 points per different coin type in your deck.";
        public override string HeadsDescription => "Score 2 points per different coin type in your deck.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Gold(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Orchestra Heads with at least four distinct coins", 10, 35, 100,
            "Heads scores +2 points.", "Tails gains +1 gold.", "Heads scores another +3 points.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads);

        public OrchestraCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) =>
            {
                if (e.Inst != ctx.Coin || e.Res?.Result != Side.Heads) return;
                var ids = new HashSet<string>();
                foreach (var coin in ctx.Game.Coins) ids.Add(coin.Id);
                if (ids.Count >= 4) ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads)
                {
                    if (level >= 1) res.Effects.Add(Effect.Score(2));
                    if (level >= 3) res.Effects.Add(Effect.Score(3));
                }
                else if (res.Result == Side.Tails && level >= 2) res.Effects.Add(Effect.Gold(1));
            };
        }

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails)
        {
            var definitions = new HashSet<CoinDef>();
            foreach (var coin in game.Coins) definitions.Add(coin.Definition);
            return heads * 2 * definitions.Count;
        }

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            var ids = new HashSet<string>(); foreach (var coin in game.Coins) ids.Add(coin.Id);
            res.Effects.Add(Effect.Score( 2 * ids.Count));
        }
    }
}
