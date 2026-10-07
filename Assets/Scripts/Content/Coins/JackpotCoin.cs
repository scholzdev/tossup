using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class JackpotCoin : CoinDef
    {
        public override string Id => "jackpot";
        public override string Name => "Jackpot";
        public override string Description => "Heads: 25 points. Only 15% Heads.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 18;
        public override int EnergyCost => 1;
        public override double Probability => 0.15;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(25) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Jackpot Heads landed", 3, 10, 30,
            "Heads scores +2 points.", "Heads scores another +3 points.", "Heads scores another +5 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public JackpotCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Res?.Result == Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads) return;
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(2));
                if (level >= 2) res.Effects.Add(Effect.Score(3));
                if (level >= 3) res.Effects.Add(Effect.Score(5));
            };
        }
    }
}
