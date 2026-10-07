using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class BankCoin : CoinDef
    {
        public override string Id => "bank";
        public override string Name => "Bank";
        public override string Description => "Heads: +3 gold, plus 1 per 10 gold held (max +3).";
        public override string SpecialRule => "Gain up to 3 bonus gold based on how much gold you already hold.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.4;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Fortune, CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Gold(3) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            double interest = Math.Min(3, Math.Floor(game.Player.Gold / 10));
            if (interest > 0) res.Effects.Add(Effect.Gold( interest));
        }
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Heads landed while holding at least 20 gold", 20, 80, 240,
            "Interest cap rises to +4 gold.", "Interest cap rises to +5 gold.", "Interest cap rises to +6 gold.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public BankCoin()
        {
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Game.Player.Gold >= 20) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Gold(Math.Max(0, Math.Min(3 + ctx.Mastery.Level, Math.Floor(ctx.Game.Player.Gold / 10)) - Math.Min(3, Math.Floor(ctx.Game.Player.Gold / 10))))); };
        }
    }
}
