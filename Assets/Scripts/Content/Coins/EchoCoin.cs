using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class EchoCoin : CoinDef
    {
        public override string Id => "echo";
        public override string Name => "Echo";
        public override string Description => "Repeats the effects the previous coin had for this side.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.6;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            var previous = game.LastResult;
            if (previous == null) return;
            var def = Game.GetCoin(game, previous.Uid).Definition;
            foreach (var effect in res.Result == Side.Heads ? def.Heads : def.Tails)
                res.Effects.Add(effect.Copy());
        }
    }
}
