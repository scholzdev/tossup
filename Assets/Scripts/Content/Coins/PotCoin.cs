using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class PotCoin : CoinDef
    {
        public override string Id => "pot";
        public override string Name => "Pot";
        public override string Description => "Heads: points equal to the flips made so far this level (max 8).";
        public override string HeadsDescription => "Score 1 point per flip so far this level (max 8).";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.25;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState g, CoinInst i, Res r)
        {if(r.Result==Side.Heads)r.Effects.Add(Effect.Score(Math.Min(8,g.Encounter.Flips)));}
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Heads landed after at least six flips this level", 15, 60, 180,
            "Its Heads score cap rises to 9.", "Its Heads score cap rises to 10.", "Its Heads score cap rises to 12.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public PotCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Flip.Final == Side.Heads && ctx.Game.Encounter.Flips >= 6) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(Math.Max(0, Math.Min(ctx.Mastery.Level >= 3 ? 12 : 8 + ctx.Mastery.Level, ctx.Game.Encounter.Flips) - Math.Min(8, ctx.Game.Encounter.Flips)))); };
        }
    }
}
