using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class VampireCoin : CoinDef
    {
        public override string Id => "vampire";
        public override string Name => "Vampire";
        public override string Description => "Heads: 2 points and it drains 2 gold from the house.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.35;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Blood };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(2), Effect.Gold(2) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Vampire Heads landed", 15, 55, 160,
            "Heads drains +1 gold.", "Heads scores +1 point.", "Heads chance +3%.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public VampireCoin()
        {
            On.Game.Coins.Resolved += (ctx,e) => { if(e.Inst==ctx.Coin && e.Res.Result==Side.Heads) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx,res) => { if(res.Result!=Side.Heads) return; if(ctx.Mastery.Level>=1) res.Effects.Add(Effect.Gold(1)); if(ctx.Mastery.Level>=2) res.Effects.Add(Effect.Score(1)); };
            On.Coins.Odds += (ctx,odds) => { if(ctx.Mastery.Level>=3) odds.Heads+=.03; };
        }
    }
}
