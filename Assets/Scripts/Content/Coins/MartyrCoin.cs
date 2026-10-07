using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MartyrCoin : CoinDef
    {
        public override string Id => "martyr";
        public override string Name => "Martyr";
        public override string Description => "Tails: enemy +3. Heads: 4 points, +1 per Tails so far this level.";
        public override string HeadsDescription => "Score 4 points, plus 1 for every Tails this level.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Penalty(3) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState g, CoinInst i, Res r)
        {if(r.Result==Side.Heads&&g.Encounter.Tails>0)r.Effects.Add(Effect.Score(g.Encounter.Tails));}

        public override void Grow(CoinInst inst, CoinGrowthEvent evt)
        { if (evt == CoinGrowthEvent.Level) inst.Debt = 0; }
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Heads landed after Tails this level", 20, 80, 240,
            "Those Heads score +1.", "Those Heads score +2 total.", "Those Heads score +3 total.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public MartyrCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Flip.Final == Side.Heads && ctx.Game.Encounter.Tails > 0) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Game.Encounter.Tails > 0 && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level)); };
        }
    }
}
