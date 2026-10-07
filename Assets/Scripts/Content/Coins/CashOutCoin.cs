using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CashOutCoin : CoinDef
    {
        public override string Id => "cash_out";
        public override string Name => "Cash Out";
        public override string Description => "Heads: 3 points, squares the combo multiplier, banks its pot, then resets the combo.";
        public override string HeadsDescription => "Score 3 points, square the combo payout, bank its pot, then reset the combo.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Cash Out Heads at combo length 3 or more", 10, 35, 100,
            "Heads scores +1 point.", "Heads also gains 1 gold.", "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public CashOutCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Res?.Result == Side.Heads && e.Flip?.Combo?.Len >= 3) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads) return;
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(1));
                if (level >= 2) res.Effects.Add(Effect.Gold(1));
                if (level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        { if (res.Result == Side.Heads) res.CashOut = true; }
    }
}
