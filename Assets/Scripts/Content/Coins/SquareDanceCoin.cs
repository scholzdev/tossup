using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class SquareDanceCoin : CoinDef
    {
        public override string Id => "square_dance";
        public override string Name => "Square Dance";
        public override string Description => "Heads: 2 points times the square of Square Dance copies in your deck.";
        public override Rarity Rarity => Rarity.Common;
        public override int Cost => 14;
        public override int EnergyCost => 0;
        public override double Probability => 0.27;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override string HeadsDescription => "1/2/3 copies: 2/8/18 points";
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Square Dance Heads with at least two copies", 10, 35, 100,
            "Heads scores +1 point per copy.", "Heads chance +3%.",
            "Heads scores another +1 point per copy.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public SquareDanceCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) =>
            {
                if (e.Inst != ctx.Coin || e.Res.Result != Side.Heads) return;
                int copies = 0;
                foreach (var coin in ctx.Game.Coins)
                    if (coin.Definition == this) copies++;
                if (copies >= 2) ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads || ctx.Mastery.Level == 0) return;
                int copies = 0;
                foreach (var coin in ctx.Game.Coins)
                    if (coin.Definition == this) copies++;
                res.Effects.Add(Effect.Score(copies * (ctx.Mastery.Level >= 3 ? 2 : 1)));
            };
            On.Coins.Odds += (ctx, odds) =>
            { if (ctx.Mastery.Level >= 2) odds.Heads += .03; };
        }

        public override double EstimateExtraScore(GameState game, CoinInst inst, double heads, double tails)
        {
            int copies = 0;
            foreach (var coin in game.Coins) if (coin.Definition == this) copies++;
            return heads * 2 * copies * copies;
        }

        public override void OnResolve(GameState g, CoinInst i, Res r)
        { if(r.Result==Side.Heads){ int n=0; foreach(var c in g.Coins) if(c.Id==i.Id)n++; r.Effects.Add(Effect.Score(2*n*n)); } }
    }
}
