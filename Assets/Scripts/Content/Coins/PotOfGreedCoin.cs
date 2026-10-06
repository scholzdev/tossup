using System;
using System.Collections.Generic;

namespace Tossup.Coins
{
    public sealed class PotOfGreedCoin : CoinDef
    {
        public override string Id => "potofgreed";
        public override string Name => "Pot of Greed";
        public override string Description => "Each side grants up to 2 random coins of a different rarity.";
        public override string HeadsDescription => "Gain up to 2 random Common coins.";
        public override string TailsDescription => "Gain up to 2 random Rare coins.";
        public override string EdgeDescription => "Gain up to 2 random Epic coins.";
        public override string SpecialRule => "Granted coins expand your deck slots, up to the 10-coin limit.";
        public override Rarity Rarity => Rarity.Rare;
        public override int Cost => 5;
        public override int EnergyCost => 2;
        public override double Probability => 1.0 / 3.0;
        public override double TieProbability => 1.0 / 3.0;
        public override IReadOnlyList<CoinType> Types { get; } = new [] { CoinType.Greed };
        public override IReadOnlyList<Effect> Heads { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Tails { get; } = Array.Empty<Effect>();
        public override IReadOnlyList<Effect> Edge { get; } = Array.Empty<Effect>();

        public override void OnResolve(GameState game, CoinInst inst, Res res)
        {
            Rarity rarity;
            if (res.Result == Side.Heads) rarity = Rarity.Common;
            else if (res.Result == Side.Tails) rarity = Rarity.Rare;
            else if (res.Result == Side.Tie) rarity = Rarity.Epic;
            else return;

            var pool = new List<CoinDef>();
            foreach (var coin in Content.CoinOrder)
                if (coin.Rarity == rarity && coin.Id != Id)
                    pool.Add(coin);

            if (pool.Count == 0) return;
            for (int n = 0; n < 2 && game.Coins.Count < Game.DeckMax; n++)
                Game.TryGrantCoin(game, pool[Rng.Int(game, 1, pool.Count) - 1]);
        }
    }
}
