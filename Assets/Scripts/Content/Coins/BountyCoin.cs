using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BountyCoin : CoinDef
    {
        public override string Id => "bounty";
        public override string Name => "Bounty";
        public override string Description => "Pays 1 gold for every 2 points it scores.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(4) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(2) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void Register(CoinCtx ctx)
        { ctx.On(GameSignal.EffectApplied, e =>
        {
            if (e.Effect.Type == EffectType.Score) e.Game.Player.Gold += Math.Floor(e.Effect.Amount / 2);
        }); }
    }
}
