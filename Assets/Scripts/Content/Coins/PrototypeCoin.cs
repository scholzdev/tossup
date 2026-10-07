using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class PrototypeCoin : CoinDef
    {
        public override string Id => "prototype";
        public override string Name => "Prototype";
        public override string Description => "Each result triggers a seeded random output: 3 points, 2 gold, or 1 energy.";
        public override string HeadsDescription => "Trigger a random output: 3 points, 2 gold, or 1 energy.";
        public override string TailsDescription => "Trigger a random output: 3 points, 2 gold, or 1 energy.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 20;
        public override int EnergyCost => 1;
        public override double Probability => 0.5;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos, CoinType.Steel };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Array.Empty<Effect>();
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Prototype random outputs triggered", 20, 65, 200,
            "Score output rises to 4 points.", "Gold output rises to 3 gold.", "Energy output rises to 2 energy.",
            MasterySides.All, MasterySides.All, MasterySides.All);

        public PrototypeCoin()
        {
            On.Coins.Resolve += (context, result) =>
            {
                context.Mastery.Add(1);
                switch (Rng.Int(context.Game, 1, 3))
                {
                    case 1: result.Effects.Add(Effect.Score(context.Mastery.Level >= 1 ? 4 : 3)); break;
                    case 2: result.Effects.Add(Effect.Gold(context.Mastery.Level >= 2 ? 3 : 2)); break;
                    default: result.Effects.Add(Effect.Energy(context.Mastery.Level >= 3 ? 2 : 1)); break;
                }
            };
        }
    }
}
