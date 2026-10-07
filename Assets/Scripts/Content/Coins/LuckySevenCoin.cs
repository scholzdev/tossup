using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class LuckySevenCoin : CoinDef
    {
        public override string Id => "lucky_seven";
        public override string Name => "Lucky Seven";
        public override string Description => "1 in 7: lands Heads and pays triple points.";
        public override string SpecialRule => "One flip in seven is forced to Heads and triples the payout.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 15;
        public override int EnergyCost => 0;
        public override double Probability => 0.3;
        public override double TieProbability => 0;
        public override IReadOnlyList<CoinType> Types { get; } = Array.Empty<CoinType>();
        public override IReadOnlyList<Effect> Heads { get; } = new[] { Effect.Score(3) };
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge => Effect.HalfOf(Tails, Heads);

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            if (!inst.Jackpot) return;
            inst.Jackpot = false;
            foreach (var effect in res.Effects) effect.Amount *= 3;
        }

        public override void OnFlip(GameState game, CoinInst inst, FlipState flip)
        {
            inst.Jackpot = Rng.Int(game, 1, 7) == 7;
            if (inst.Jackpot) flip.Result = Side.Heads;
        }
        public override CoinMastery Mastery { get; } = new CoinMastery(
            "Lucky Seven jackpots triggered", 7, 28, 84,
            "Jackpots score +1.", "Jackpots score +2 total.", "Jackpots score +3 total.",
            MasterySides.Heads, MasterySides.Heads, MasterySides.Heads);

        public LuckySevenCoin()
        {
            On.Coins.Flip += (ctx, flip) => { if (ctx.Coin.Jackpot) ctx.Mastery.Add(1); };
            On.Coins.Resolve += (ctx, res) => { if (res.Result == Side.Heads && res.Effects.Count > 0 && res.Effects[0].Type == EffectType.Score && res.Effects[0].Amount >= 9 && ctx.Mastery.Level > 0) res.Effects.Add(Effect.Score(ctx.Mastery.Level)); };
        }
    }
}
