using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class TwinCoin : CoinDef
    {
        public override string Id => "twin";
        public override string Name => "Twin";
        public override string Description => "Always lands the same as the previous flip.";
        public override string SpecialRule => "Always lands the same as the previous flip.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnFlip(GameState game, CoinInst inst, FlipState flip)
        {
            var previous = game.LastResult;
            if (previous != null) flip.Result = previous.Final;
        }
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Previous outcomes matched", 20, 80, 240,
            "Matched Heads scores +1.", "Matched Tails scores +1.", "Both matched outcomes score +1 more.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads | MasterySides.Tails);

        public TwinCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && ctx.Game.LastResult != null && e.Flip.Final == ctx.Game.LastResult.Final) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (ctx.Game.LastResult == null || res.Result != ctx.Game.LastResult.Final) return; if (res.Result == Side.Heads && ctx.Mastery.Level >= 1) res.Effects.Add(Effect.Score(ctx.Mastery.Level >= 3 ? 2 : 1)); if (res.Result == Side.Tails && ctx.Mastery.Level >= 2) res.Effects.Add(Effect.Score(ctx.Mastery.Level >= 3 ? 2 : 1)); };
        }
    }
}
