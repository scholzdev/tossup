using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MimicCoin : CoinDef
    {
        public override string Id => "mimic";
        public override string Name => "Mimic";
        public override string Description => "Heads: does what the Heads side of a random other coin in your deck does.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            var choices = new List<CoinInst>();
            foreach (var coin in game.Coins) if (coin != inst && coin.Definition.Heads.Count > 0) choices.Add(coin);
            if (choices.Count == 0) return;
            var pick = choices[Rng.Int(game, 1, choices.Count) - 1];
            foreach (var effect in pick.Definition.Heads) res.Effects.Add(effect.Copy());
            Game.Log(game, "Mimic copies " + pick.Definition.Name + ".");
        }
    }
}
