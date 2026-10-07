using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class CounterpointCoin : CoinDef
    {
        public override string Id => "counterpoint";
        public override string Name => "Counterpoint";
        public override string Description => "When its result differs from the previous coin, score 5 points.";
        public override string SpecialRule => "A Heads or Tails result that differs from the previous coin scores 5 points.";
        public override string HeadsDescription => "Score 5 points if the previous coin landed Tails.";
        public override string TailsDescription => "Score 5 points if the previous coin landed Heads.";
        public override string EdgeDescription => "No bonus on Edge.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 20;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0.1;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge { get; } = Array.Empty<Effect>();

        public CounterpointCoin()
        {
            On.Coins.Resolve += (context, result) =>
            {
                var previous = context.Game.LastResult;
                if (previous != null && result.Result != Side.Tie && previous.Final != result.Result)
                    result.Effects.Add(Effect.Score(5));
            };
            On.Coins.Resolve += (ctx, res) =>
            {
                var previous = ctx.Game.LastResult;
                if (previous == null || res.Result == Side.Tie || previous.Final == res.Result) return;
                ctx.Mastery.Add(1);
                if (ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level));
            };
        }

        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Alternating results scored by Counterpoint", 20, 80, 240,
            "Alternating results score +1.", "Alternating results score +2 total.", "Alternating results score +3 total.",
            MasterySides.Heads | MasterySides.Tails, MasterySides.Heads | MasterySides.Tails, MasterySides.Heads | MasterySides.Tails);
    }
}
