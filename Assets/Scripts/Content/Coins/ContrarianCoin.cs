using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class ContrarianCoin : CoinDef
    {
        public override string Id => "contrarian";
        public override string Name => "Contrarian";
        public override string Description => "Always lands opposite of the previous flip.";
        public override string SpecialRule => "Always lands opposite of the previous flip.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.65;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnFlip(GameState game, CoinInst inst, FlipState flip)
        {
            var previous = game.LastResult;
            if (previous != null) flip.Result = previous.Final == Side.Heads ? Side.Tails : Side.Heads;
        }
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Previous flips reversed", 20, 80, 240,
            "Opposite-side Heads scores +1.", "Opposite-side Tails scores +1.", "Both opposite-side results score +1 more.",
            MasterySides.Heads, MasterySides.Tails, MasterySides.Heads | MasterySides.Tails);

        public ContrarianCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && ctx.Game.LastResult != null) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (ctx.Game.LastResult == null) return;
                int level = ctx.Mastery.Level;
                if (res.Result == Side.Heads && level >= 1 || res.Result == Side.Tails && level >= 2)
                    res.Effects.Add(Effect.Score(level >= 3 ? 2 : 1));
            };
        }
    }
}
