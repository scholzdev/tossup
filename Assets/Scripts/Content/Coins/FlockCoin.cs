using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class FlockCoin : CoinDef
    {
        public override string Id => "flock";
        public override string Name => "Flock";
        public override string Description => "+10% Heads for every other Flock in your deck.";
        public override string SpecialRule => "+10% Heads for every other Flock in your deck.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public FlockCoin()
        {
            On.Coins.Odds += (ctx, odds) =>
            {
                foreach (var other in ctx.Game.Coins)
                    if (other != ctx.Coin && other.Id == ctx.Coin.Id) odds.Heads += .1;
            };
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst != ctx.Coin || e.Flip.Final != Side.Heads) return; foreach (var other in ctx.Game.Coins) if (other != ctx.Coin && other.Id == ctx.Coin.Id) { ctx.Mastery.Add(1); break; } };
            On.Coins.Odds += (ctx, odds) => { foreach (var other in ctx.Game.Coins) if (other != ctx.Coin && other.Id == ctx.Coin.Id) odds.Heads += .02 * ctx.Mastery.Level; };
        }

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Heads landed with another Flock in the deck", 20, 80, 240,
            "Each other Flock adds +12% Heads instead of +10%.", "Each other Flock adds +14% Heads.", "Each other Flock adds +16% Heads.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);
    }
}
