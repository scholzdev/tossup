using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class TrueEchoCoin : CoinDef
    {
        public override string Id => "true_echo";
        public override string Name => "True Echo";
        public override string Description => "Repeats what the previous coin really did, including its buffs and growth, on either side.";
        public override string SpecialRule => "Repeats the previous coin's resolved effects, buffs, and growth.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Previous coin effects copied by True Echo", 20, 75, 220,
            "A successful copy scores +1 point.", "A successful copy gains 1 gold.", "A successful copy scores another +2 points.",
            MasterySides.All, MasterySides.All, MasterySides.All);

        public TrueEchoCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) =>
            {
                if (e.Inst == ctx.Coin && e.Flip?.BaseEffects != null && e.Flip.BaseEffects.Count > 0)
                    ctx.Mastery.Add(e.Flip.BaseEffects.Count);
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (ctx.Game.LastResult?.BaseEffects == null || ctx.Game.LastResult.BaseEffects.Count == 0) return;
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(1));
                if (level >= 2) res.Effects.Add(Effect.Gold(1));
                if (level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            var previous = game.LastResult;
            if (previous == null || previous.BaseEffects == null) return;
            foreach (var effect in previous.BaseEffects) res.Effects.Add(effect.Copy());
            if (previous.BaseBuffs != null)
                foreach (var buff in previous.BaseBuffs) res.Buffs.Add(buff.Copy());
        }
    }
}
