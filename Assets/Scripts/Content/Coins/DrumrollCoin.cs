using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class DrumrollCoin : CoinDef
    {
        public override string Id => "drumroll";
        public override string Name => "Drumroll";
        public override string Description => "Heads: 1 point. Every third flip, score 3 additional points.";
        public override string HeadsDescription => "Score 1 point, plus 3 points every third flip.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override int Cost => 16;
        public override int EnergyCost => 0;
        public override double Probability => 0.55;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Rhythm };
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Array.Empty<Effect>();
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Drumroll third-flip bonuses triggered", 10, 35, 100,
            "Each third flip scores +1 point.", "Heads scores +1 point.", "Each third flip scores another +2 points.",
            MasterySides.All, MasterySides.Heads, MasterySides.All);

        public DrumrollCoin()
        {
            On.Coins.Resolve += (context, result) =>
            {
                if (context.Game.Encounter.Flips % 3 == 0)
                {
                    context.Mastery.Add(1);
                    result.Effects.Add(Effect.Score(3));
                    if (context.Mastery.Level >= 1) result.Effects.Add(Effect.Score(1));
                    if (context.Mastery.Level >= 3) result.Effects.Add(Effect.Score(2));
                }
                if (result.Result == Side.Heads && context.Mastery.Level >= 2) result.Effects.Add(Effect.Score(1));
            };
        }
    }
}
