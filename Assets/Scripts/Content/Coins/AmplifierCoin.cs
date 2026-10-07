using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class AmplifierCoin : CoinDef
    {
        public override string Id => "amplifier";
        public override string Name => "Amplifier";
        public override string Description => "Heads: 1 point. Active buffs last 1 coin longer; odds and multipliers grow stronger. Tails: 1 point.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 1;
        public override double Probability => 0.7;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1), Effect.Amplify(1) };
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Amplifier Heads while buffs are active", 10, 35, 100,
            "Heads scores +1 point.", "Heads extends buffs by another coin.", "Heads scores another +2 points.",
            MasterySides.Heads, MasterySides.None, MasterySides.Heads);

        public AmplifierCoin()
        {
            On.Game.Effects.Applied += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Effect?.Type == EffectType.Amplify && e.BuffsAffected > 0)
                    ctx.Mastery.Add(1);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads) return;
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(1));
                if (level >= 2)
                    foreach (var buff in ctx.Game.Encounter.Buffs) buff.Left++;
                if (level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }
    }
}
