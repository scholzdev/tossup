using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class MimicCoin : CoinDef
    {
        public override string Id => "mimic";
        public override string Name => "Mimic";
        public override string Description => "Heads: does what the Heads side of a random other coin in your deck does.";
        public override string HeadsDescription => "Copy the Heads effects of a random other coin in your deck.";
        public override Rarity Rarity => Rarity.Epic;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = new[] { CoinType.Chaos };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = new[] { Effect.Score(1) };
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Other coins copied on Mimic Heads", 10, 35, 100,
            "A successful copy scores +1 point.", "A successful copy gains 1 gold.", "A successful copy scores another +2 points.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public MimicCoin()
        {
            On.Game.Coins.Resolved += (ctx, e) => { if (e.Inst == ctx.Coin && e.Res?.Result == Side.Heads && e.Flip?.BaseEffects?.Count > 0) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) =>
            {
                if (res.Result != Side.Heads || res.Effects.Count == 0) return;
                int level = ctx.Mastery.Level;
                if (level >= 1) res.Effects.Add(Effect.Score(1));
                if (level >= 2) res.Effects.Add(Effect.Gold(1));
                if (level >= 3) res.Effects.Add(Effect.Score(2));
            };
        }

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
