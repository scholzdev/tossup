using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class EchoCoin : CoinDef
    {
        public override string Id => "echo";
        public override string Name => "Echo";
        public override string Description => "Repeats the effects the previous coin had for this side.";
        public override string HeadsDescription => "Repeat the previous coin's Heads effects.";
        public override string TailsDescription => "Repeat the previous coin's Tails effects.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Previous effects repeated by Echo", 20, 70, 200,
            "Successful repeats score +1 point.", "Heads chance +8%.",
            "Successful repeats score another +2 points.",
            MasterySides.All, MasterySides.Heads, MasterySides.All);

        public EchoCoin()
        {
            On.Coins.Resolve += (ctx, res) =>
            {
                int repeated = res.Effects.Count + res.Buffs.Count;
                if (repeated <= 0) return;
                ctx.Mastery.Add(repeated);
                if (ctx.Mastery.Level >= 1)
                    res.Effects.Add(Effect.Score(ctx.Mastery.Level >= 3 ? 3 : 1));
            };
            On.Coins.Odds += (ctx, odds) =>
            { if (ctx.Mastery.Level >= 2) odds.Heads += .08; };
        }

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            var previous = game.LastResult;
            if (previous == null) return;
            var previousCoin = Game.GetCoin(game, previous.Uid);
            if (previousCoin == null) return;
            var def = previousCoin.Definition;
            var side = res.Result == Side.Heads ? OutcomeSide.Heads : OutcomeSide.Tails;
            foreach (var effect in def.EffectsFor(side))
                res.Effects.Add(effect.Copy());
            foreach (var buff in def.BuffsFor())
                if (buff.Trigger == side) res.Buffs.Add(buff.Copy());
        }
    }
}
